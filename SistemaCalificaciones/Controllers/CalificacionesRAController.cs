using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;
using System.Security.Claims;

namespace SistemaCalificaciones.Controllers;

/// <summary>
/// Nota anual de las materias técnicas (por Resultados de Aprendizaje).
/// El cálculo está en CalculoRAService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Maestro")]
public class CalificacionesRAController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalculoRAService _calculoRA;

    public CalificacionesRAController(AppDbContext context, CalculoRAService calculoRA)
    {
        _context = context;
        _calculoRA = calculoRA;
    }

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(int idAsignacionDocente)
    {
        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        if (!asignacion.Materia.EsTecnica)
            return BadRequest("Esta asignación no pertenece a una materia técnica.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(asignacion))
            return Forbid();

        var resultado = await _calculoRA.CalcularAsync(idAsignacionDocente);

        if (!resultado.Calculado)
            return BadRequest(resultado.Mensaje);

        return Ok(new
        {
            mensaje = resultado.Mensaje,
            estudiantes = resultado.Estudiantes,
            cambiosEnPublicadas = resultado.CambiosEnPublicadas
        });
    }

    // Un solo botón: guarda las notas de las actividades RA de todos los estudiantes
    // y en la misma llamada calcula la nota RA de cada estudiante.
    // Nota nula = casilla vacía: borra la nota existente de esa actividad para ese estudiante.
    [HttpPost("guardar-y-calcular")]
    public async Task<IActionResult> GuardarYCalcular([FromBody] GuardarYCalcularRADto dto)
    {
        if (dto.IdAsignacionDocente <= 0)
            return BadRequest("Debes indicar una asignación docente válida.");

        var notas = dto.Notas ?? new List<NotaActividadRADto>();

        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == dto.IdAsignacionDocente);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        if (!asignacion.Materia.EsTecnica)
            return BadRequest("Esta asignación no pertenece a una materia técnica.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(asignacion))
            return Forbid();

        if (notas.GroupBy(n => new { n.IdActividadCompetencia, n.IdEstudiante }).Any(g => g.Count() > 1))
            return BadRequest("La solicitud contiene notas duplicadas para una actividad y un estudiante.");

        if (notas.Any(n => n.Nota.HasValue && (n.Nota.Value < 0 || n.Nota.Value > 100)))
            return BadRequest("Las notas deben estar entre 0 y 100.");

        var idsActividades = notas.Select(n => n.IdActividadCompetencia).Distinct().ToList();
        var idsEstudiantes = notas.Select(n => n.IdEstudiante).Distinct().ToList();

        var actividadesValidas = await _context.ActividadesCompetencias
            .Where(a =>
                idsActividades.Contains(a.IdActividadCompetencia) &&
                a.IdAsignacionDocente == dto.IdAsignacionDocente &&
                a.IdResultadoAprendizaje != null &&
                a.Activa)
            .Select(a => a.IdActividadCompetencia)
            .ToListAsync();

        if (actividadesValidas.Count != idsActividades.Count)
            return BadRequest("Una o más actividades no existen, están inactivas o no son actividades RA de esta asignación.");

        var estudiantesValidos = await _context.Inscripciones
            .Where(i =>
                idsEstudiantes.Contains(i.IdEstudiante) &&
                i.IdCurso == asignacion.IdCurso &&
                i.IdAnioEscolar == asignacion.IdAnioEscolar &&
                i.Estado == "Activo" && i.Estudiante.Activo)
            .Select(i => i.IdEstudiante)
            .Distinct()
            .ToListAsync();

        if (estudiantesValidos.Count != idsEstudiantes.Count)
            return BadRequest("Una o más personas no están inscritas activamente en el curso de esta asignación.");

        var existentes = await _context.NotasCompetencias
            .Where(n => idsActividades.Contains(n.IdActividadCompetencia))
            .ToListAsync();

        int guardadas = 0, eliminadas = 0;
        var ahora = DateTime.Now;

        foreach (var item in notas)
        {
            var actual = existentes.FirstOrDefault(n =>
                n.IdActividadCompetencia == item.IdActividadCompetencia &&
                n.IdEstudiante == item.IdEstudiante);

            if (!item.Nota.HasValue)
            {
                if (actual != null)
                {
                    _context.NotasCompetencias.Remove(actual);
                    eliminadas++;
                }
                continue;
            }

            if (actual == null)
            {
                _context.NotasCompetencias.Add(new NotaCompetencia
                {
                    IdActividadCompetencia = item.IdActividadCompetencia,
                    IdEstudiante = item.IdEstudiante,
                    Nota = item.Nota.Value,
                    FechaRegistro = ahora
                });
                guardadas++;
            }
            else if (actual.Nota != item.Nota.Value)
            {
                actual.Nota = item.Nota.Value;
                actual.FechaRegistro = ahora;
                guardadas++;
            }
        }

        await _context.SaveChangesAsync();

        // Las notas quedan guardadas aunque el cálculo no se pueda hacer (p. ej. si los RA no suman 100).
        var calculo = await _calculoRA.CalcularAsync(dto.IdAsignacionDocente);

        return Ok(new
        {
            mensaje = calculo.Calculado
                ? $"Notas guardadas. {calculo.Mensaje}"
                : $"Notas guardadas, pero no se pudo calcular: {calculo.Mensaje}",
            notasGuardadas = guardadas,
            notasEliminadas = eliminadas,
            calculado = calculo.Calculado,
            estudiantes = calculo.Estudiantes,
            cambiosEnPublicadas = calculo.CambiosEnPublicadas
        });
    }

    public class GuardarYCalcularRADto
    {
        public int IdAsignacionDocente { get; set; }
        public List<NotaActividadRADto> Notas { get; set; } = new();
    }

    public class NotaActividadRADto
    {
        public int IdActividadCompetencia { get; set; }
        public int IdEstudiante { get; set; }
        public decimal? Nota { get; set; }
    }

    [HttpPut("publicar")]
    public async Task<IActionResult> Publicar(int idAsignacionDocente)
    {
        var asignacion = await _context.AsignacionesDocentes
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(asignacion))
            return Forbid();

        var calificaciones = await _context.CalificacionesRA
            .Where(c => c.IdAsignacionDocente == idAsignacionDocente)
            .ToListAsync();

        if (!calificaciones.Any())
            return BadRequest("Primero debes calcular las calificaciones RA.");

        var ahora = DateTime.Now;

        foreach (var c in calificaciones.Where(c => !c.Publicada))
        {
            c.Publicada = true;
            c.FechaPublicacion = ahora;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = $"Calificaciones RA publicadas ({calificaciones.Count} estudiante(s)).",
            publicadas = calificaciones.Count
        });
    }

    [HttpGet("asignacion/{idAsignacionDocente}")]
    public async Task<IActionResult> Estado(int idAsignacionDocente)
    {
        var asignacion = await _context.AsignacionesDocentes
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(asignacion))
            return Forbid();

        var calificaciones = await _context.CalificacionesRA
            .Where(c => c.IdAsignacionDocente == idAsignacionDocente)
            .Select(c => new
            {
                c.IdEstudiante,
                c.NotaFinal,
                c.Publicada,
                c.FechaCalculo,
                c.FechaPublicacion
            })
            .ToListAsync();

        return Ok(new
        {
            calculadas = calificaciones.Count,
            publicadas = calificaciones.Count(c => c.Publicada),
            ultimoCalculo = calificaciones.Max(c => (DateTime?)c.FechaCalculo),
            ultimaPublicacion = calificaciones.Max(c => c.FechaPublicacion),
            calificaciones
        });
    }

    private async Task<bool> EsDuenoAsync(AsignacionDocente asignacion)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario))
            return false;

        return await _context.Maestros.AnyAsync(m =>
            m.IdUsuario == idUsuario &&
            m.IdMaestro == asignacion.IdMaestro);
    }
}
