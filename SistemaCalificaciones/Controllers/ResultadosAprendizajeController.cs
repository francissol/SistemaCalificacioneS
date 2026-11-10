using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;
using System.Security.Claims;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Maestro,Administrador,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico")]
public class ResultadosAprendizajeController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResultadosAprendizajeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("asignacion/{idAsignacion}")]
    public async Task<IActionResult> ObtenerRA(int idAsignacion)
    {
        var ras = await _context.ResultadosAprendizaje
            .Where(r =>
                r.IdAsignacionDocente == idAsignacion &&
                r.Activo)
            .OrderBy(r => r.Codigo)
            .Select(r => new
            {
                r.IdResultadoAprendizaje,
                r.IdAsignacionDocente,
                r.Codigo,
                r.Nombre,
                r.ValorMaximo,
                r.Activo
            })
            .ToListAsync();

        var total = ras.Sum(r => r.ValorMaximo);

        return Ok(new
        {
            total,
            faltante = 100 - total,
            completo = total == 100,
            resultados = ras
        });
    }

    [HttpPost("asignacion/{idAsignacion}")]
    public async Task<IActionResult> CrearRA(
        int idAsignacion,
        CrearRADto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre del RA es obligatorio.");

        if (dto.ValorMaximo <= 0)
            return BadRequest("El valor máximo debe ser mayor que 0.");

        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Materia)
            .Include(a => a.Curso)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacion);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        if (!asignacion.Materia.EsTecnica)
            return BadRequest("Esta materia no es técnica.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(asignacion))
            return Forbid();

        string codigo;
        if (string.IsNullOrWhiteSpace(dto.Codigo))
        {
            var cantidad = await _context.ResultadosAprendizaje
                .CountAsync(r => r.IdAsignacionDocente == idAsignacion);
            codigo = $"RA{cantidad + 1}";
        }
        else
        {
            codigo = dto.Codigo.Trim().ToUpper();
            if (codigo.Length > 10)
                return BadRequest("El código no puede tener más de 10 caracteres.");
        }

        if (await ExisteCodigoAsync(idAsignacion, codigo, null))
            return BadRequest($"Ya existe un RA activo con el código {codigo} en esta asignación.");

        // Asignación de origen + (opcional) los otros cursos del mismo maestro, materia y año.
        var destinos = new List<AsignacionDocente> { asignacion };
        var omitidos = new List<object>();

        if (dto.AplicarATodos)
        {
            var otras = await OtrasAsignacionesAsync(asignacion);

            foreach (var otra in otras)
            {
                if (await ExisteCodigoAsync(otra.IdAsignacionDocente, codigo, null))
                    omitidos.Add(new { otra.IdAsignacionDocente, Curso = NombreCurso(otra), Motivo = $"Ya tiene un {codigo}." });
                else
                    destinos.Add(otra);
            }
        }

        // La suma de RA nunca puede pasar de 100 en ningún curso: si uno se pasa, no se crea en ninguno.
        var excedidos = new List<string>();
        foreach (var destino in destinos)
        {
            var totalActual = await TotalActivoAsync(destino.IdAsignacionDocente, null);
            if (totalActual + dto.ValorMaximo > 100)
                excedidos.Add($"{NombreCurso(destino)} (lleva {totalActual})");
        }

        if (excedidos.Any())
            return BadRequest($"La suma excede 100 en: {string.Join(", ", excedidos)}. No se creó el RA en ningún curso.");

        var creados = destinos.Select(d => new ResultadoAprendizaje
        {
            IdAsignacionDocente = d.IdAsignacionDocente,
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            ValorMaximo = dto.ValorMaximo,
            Activo = true
        }).ToList();

        _context.ResultadosAprendizaje.AddRange(creados);
        await _context.SaveChangesAsync();

        var ra = creados[0];

        return Ok(new
        {
            ra.IdResultadoAprendizaje,
            ra.IdAsignacionDocente,
            ra.Codigo,
            ra.Nombre,
            ra.ValorMaximo,
            ra.Activo,
            aplicadoEn = creados.Skip(1).Select(c => new
            {
                c.IdResultadoAprendizaje,
                c.IdAsignacionDocente,
                Curso = NombreCurso(destinos.First(d => d.IdAsignacionDocente == c.IdAsignacionDocente))
            }),
            omitidos
        });
    }

    [HttpPut("{idRA}")]
    [Authorize(Roles = "Maestro")]
    public async Task<IActionResult> EditarRA(int idRA, CrearRADto dto, [FromQuery] bool confirmar = false)
    {
        var ra = await _context.ResultadosAprendizaje
            .Include(r => r.AsignacionDocente)
                .ThenInclude(a => a.Curso)
            .FirstOrDefaultAsync(r => r.IdResultadoAprendizaje == idRA && r.Activo);

        if (ra == null)
            return NotFound("RA no encontrado.");

        // Solo el maestro dueño de la asignación puede editar.
        if (!await EsDuenoAsync(ra.AsignacionDocente))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre del RA es obligatorio.");

        if (dto.ValorMaximo <= 0)
            return BadRequest("El valor máximo debe ser mayor que 0.");

        var codigoAnterior = ra.Codigo;
        var codigoNuevo = string.IsNullOrWhiteSpace(dto.Codigo)
            ? codigoAnterior
            : dto.Codigo.Trim().ToUpper();

        if (codigoNuevo.Length > 10)
            return BadRequest("El código no puede tener más de 10 caracteres.");

        // El RA editado + (opcional) los RA con el mismo código en los otros cursos del maestro.
        var afectados = new List<ResultadoAprendizaje> { ra };

        if (dto.AplicarATodos)
        {
            var otras = await OtrasAsignacionesAsync(ra.AsignacionDocente);
            var idsOtras = otras.Select(o => o.IdAsignacionDocente).ToList();

            var rasOtros = await _context.ResultadosAprendizaje
                .Include(r => r.AsignacionDocente)
                    .ThenInclude(a => a.Curso)
                .Where(r =>
                    idsOtras.Contains(r.IdAsignacionDocente) &&
                    r.Codigo == codigoAnterior &&
                    r.Activo)
                .ToListAsync();

            afectados.AddRange(rasOtros);
        }

        // Validaciones en cada curso: código único y suma máxima de 100.
        var errores = new List<string>();
        foreach (var r in afectados)
        {
            var curso = NombreCurso(r.AsignacionDocente);

            if (codigoNuevo != codigoAnterior &&
                await ExisteCodigoAsync(r.IdAsignacionDocente, codigoNuevo, r.IdResultadoAprendizaje))
                errores.Add($"{curso}: ya existe un RA con el código {codigoNuevo}");

            var totalSinEste = await TotalActivoAsync(r.IdAsignacionDocente, r.IdResultadoAprendizaje);
            if (totalSinEste + dto.ValorMaximo > 100)
                errores.Add($"{curso}: la suma excede 100 (sin este RA lleva {totalSinEste})");
        }

        if (errores.Any())
            return BadRequest($"No se guardó ningún cambio. {string.Join("; ", errores)}.");

        // Si baja el valor máximo, avisar de los estudiantes cuyas notas ya acumuladas lo superan.
        if (!confirmar)
        {
            var conflictos = new List<object>();

            foreach (var r in afectados.Where(r => dto.ValorMaximo < r.ValorMaximo))
            {
                var estudiantes = await _context.NotasCompetencias
                    .Where(n =>
                        n.ActividadCompetencia.IdResultadoAprendizaje == r.IdResultadoAprendizaje &&
                        n.ActividadCompetencia.Activa)
                    .GroupBy(n => new { n.IdEstudiante, n.Estudiante.Nombres, n.Estudiante.Apellidos })
                    .Select(g => new
                    {
                        g.Key.IdEstudiante,
                        Estudiante = g.Key.Nombres + " " + g.Key.Apellidos,
                        NotaAcumulada = g.Sum(n => n.Nota)
                    })
                    .Where(x => x.NotaAcumulada > dto.ValorMaximo)
                    .ToListAsync();

                if (estudiantes.Any())
                    conflictos.Add(new { Curso = NombreCurso(r.AsignacionDocente), r.Codigo, Estudiantes = estudiantes });
            }

            if (conflictos.Any())
                return Conflict(new
                {
                    mensaje = $"Hay estudiantes con notas acumuladas mayores que el nuevo valor máximo ({dto.ValorMaximo}). " +
                              "Su nota en este RA quedaría topada a ese valor. Reenvíe con ?confirmar=true para guardar.",
                    conflictos
                });
        }

        foreach (var r in afectados)
        {
            r.Codigo = codigoNuevo;
            r.Nombre = dto.Nombre.Trim();
            r.ValorMaximo = dto.ValorMaximo;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            ra.IdResultadoAprendizaje,
            ra.IdAsignacionDocente,
            ra.Codigo,
            ra.Nombre,
            ra.ValorMaximo,
            ra.Activo,
            aplicadoEn = afectados.Skip(1).Select(r => new
            {
                r.IdResultadoAprendizaje,
                r.IdAsignacionDocente,
                Curso = NombreCurso(r.AsignacionDocente)
            })
        });
    }

    [HttpDelete("{idRA}")]
    public async Task<IActionResult> EliminarRA(int idRA)
    {
        var ra = await _context.ResultadosAprendizaje
            .Include(r => r.AsignacionDocente)
            .FirstOrDefaultAsync(r => r.IdResultadoAprendizaje == idRA);

        if (ra == null)
            return NotFound("RA no encontrado.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(ra.AsignacionDocente))
            return Forbid();

        // Las notas de un RA cuelgan de sus actividades, así que esto también cubre las calificaciones.
        var actividades = await _context.ActividadesCompetencias
            .CountAsync(a => a.IdResultadoAprendizaje == idRA);

        if (actividades > 0)
        {
            var notas = await _context.NotasCompetencias
                .CountAsync(n => n.ActividadCompetencia.IdResultadoAprendizaje == idRA);

            return BadRequest($"No se puede eliminar el RA {ra.Codigo}: tiene {actividades} actividad(es) y {notas} nota(s) registradas.");
        }

        ra.Activo = false;
        await _context.SaveChangesAsync();

        return Ok("RA eliminado.");
    }

    // ---------- Auxiliares ----------

    private async Task<bool> EsDuenoAsync(AsignacionDocente asignacion)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario))
            return false;

        return await _context.Maestros.AnyAsync(m =>
            m.IdUsuario == idUsuario &&
            m.IdMaestro == asignacion.IdMaestro);
    }

    private async Task<List<AsignacionDocente>> OtrasAsignacionesAsync(AsignacionDocente asignacion)
    {
        return await _context.AsignacionesDocentes
            .Include(a => a.Curso)
            .Where(a =>
                a.IdAsignacionDocente != asignacion.IdAsignacionDocente &&
                a.IdMaestro == asignacion.IdMaestro &&
                a.IdMateria == asignacion.IdMateria &&
                a.IdAnioEscolar == asignacion.IdAnioEscolar &&
                a.Activo)
            .ToListAsync();
    }

    private Task<bool> ExisteCodigoAsync(int idAsignacion, string codigo, int? excluirIdRA)
    {
        return _context.ResultadosAprendizaje.AnyAsync(r =>
            r.IdAsignacionDocente == idAsignacion &&
            r.Codigo == codigo &&
            r.Activo &&
            r.IdResultadoAprendizaje != excluirIdRA);
    }

    private async Task<decimal> TotalActivoAsync(int idAsignacion, int? excluirIdRA)
    {
        return await _context.ResultadosAprendizaje
            .Where(r =>
                r.IdAsignacionDocente == idAsignacion &&
                r.Activo &&
                r.IdResultadoAprendizaje != excluirIdRA)
            .SumAsync(r => (decimal?)r.ValorMaximo) ?? 0;
    }

    private static string NombreCurso(AsignacionDocente a)
    {
        if (a.Curso == null) return $"Asignación {a.IdAsignacionDocente}";
        return string.IsNullOrWhiteSpace(a.Curso.Seccion)
            ? a.Curso.Nombre
            : $"{a.Curso.Nombre} {a.Curso.Seccion}";
    }
}

public class CrearRADto
{
    // Opcional: al crear, si viene vacío se genera RA{n}; al editar, si viene vacío se conserva.
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = "";
    public decimal ValorMaximo { get; set; }
    // "Aplicar también a mis otros cursos de esta materia" (marcada por defecto).
    public bool AplicarATodos { get; set; } = true;
}
