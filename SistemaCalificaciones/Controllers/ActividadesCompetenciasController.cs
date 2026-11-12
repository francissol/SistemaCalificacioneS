using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.DTOs.Competencias;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;
using System.Security.Claims;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Maestro")]
public class ActividadesCompetenciasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalculoRAService _calculoRA;

    public ActividadesCompetenciasController(AppDbContext context, CalculoRAService calculoRA)
    {
        _context = context;
        _calculoRA = calculoRA;
    }

    [HttpGet("asignacion/{idAsignacionDocente}/periodo/{idPeriodoPublicacion}")]
    public async Task<IActionResult> GetPorAsignacionYPeriodo(
        int idAsignacionDocente,
        int idPeriodoPublicacion)
    {
        var actividades = await _context.ActividadesCompetencias
            .Include(a => a.Competencia)
            .Where(a =>
                a.IdAsignacionDocente == idAsignacionDocente &&
                a.IdPeriodoPublicacion == idPeriodoPublicacion &&
                a.Activa)
            .OrderBy(a => a.Competencia.Codigo)
            .ThenBy(a => a.Nombre)
            .Select(a => new
            {
                a.IdActividadCompetencia,
                a.IdAsignacionDocente,
                a.IdPeriodoPublicacion,
                a.IdCompetencia,
                Competencia = a.Competencia.Codigo + " - " + a.Competencia.Nombre,
                a.Nombre,
                a.FechaCreacion,
                a.Activa
            })
            .ToListAsync();

        return Ok(actividades);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearActividadCompetenciaDto dto)
    {
        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Curso)
                .ThenInclude(c => c.Grado)
                    .ThenInclude(g => g.Nivel)
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == dto.IdAsignacionDocente);

        if (asignacion == null || !asignacion.Activo)
            return BadRequest("La asignación docente no existe o está inactiva.");

        if (asignacion.Materia.EsTecnica)
            return BadRequest("Las materias técnicas usan Resultados de Aprendizaje.");

        if (dto.IdPeriodoPublicacion == null)
            return BadRequest("Debe seleccionar un período.");

        if (dto.IdCompetencia == null)
            return BadRequest("Debe seleccionar una competencia.");

        var periodo = await _context.PeriodosPublicacion
            .FirstOrDefaultAsync(p => p.IdPeriodoPublicacion == dto.IdPeriodoPublicacion);

        if (periodo == null || !periodo.Activo)
            return BadRequest("El período no existe o está inactivo.");

        if (DateTime.Now.Date > periodo.FechaCierre.Date)
            return BadRequest("Este período ya cerró. No puedes crear actividades.");

        var competenciaAsignada = await _context.CompetenciasGradoMateria
            .AnyAsync(cgm =>
                cgm.IdGrado == asignacion.Curso.IdGrado &&
                cgm.IdMateria == asignacion.IdMateria &&
                cgm.IdCompetencia == dto.IdCompetencia);

        if (!competenciaAsignada)
            return BadRequest("Esta competencia no está asignada a esa materia y grado.");

        var actividad = new ActividadCompetencia
        {
            IdAsignacionDocente = dto.IdAsignacionDocente,
            IdPeriodoPublicacion = dto.IdPeriodoPublicacion,
            IdCompetencia = dto.IdCompetencia,
            IdResultadoAprendizaje = null,
            Nombre = dto.Nombre,
            FechaCreacion = DateTime.Now,
            Activa = true
        };

        _context.ActividadesCompetencias.Add(actividad);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Actividad por competencia creada correctamente.",
            actividad.IdActividadCompetencia,
            actividad.Nombre
        });
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var actividad = await _context.ActividadesCompetencias.FindAsync(id);

        if (actividad == null)
            return NotFound("Actividad no encontrada.");

        actividad.Activa = !actividad.Activa;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado actualizado correctamente.",
            actividad.IdActividadCompetencia,
            actividad.Activa
        });
    }



    [HttpGet("ra/asignacion/{idAsignacionDocente}")]
    public async Task<IActionResult> ActividadesRA(int idAsignacionDocente)
    {
        var actividades = await _context.ActividadesCompetencias
            .Include(a => a.ResultadoAprendizaje)
            .Where(a =>
                a.IdAsignacionDocente == idAsignacionDocente &&
                a.IdResultadoAprendizaje != null &&
                a.Activa)
            .Select(a => new
            {
                a.IdActividadCompetencia,
                a.IdResultadoAprendizaje,
                RA = a.ResultadoAprendizaje!.Codigo,
                a.Nombre
            })
            .ToListAsync();

        return Ok(actividades);
    }

    [HttpPost("ra")]
    public async Task<IActionResult> CrearActividadRA(CrearActividadRADto dto)
    {
        var ra = await _context.ResultadosAprendizaje
            .FirstOrDefaultAsync(r =>
                r.IdResultadoAprendizaje == dto.IdResultadoAprendizaje &&
                r.IdAsignacionDocente == dto.IdAsignacionDocente &&
                r.Activo);

        if (ra == null)
            return BadRequest("RA no encontrado para esta asignación.");

        var actividad = new ActividadCompetencia
        {
            IdAsignacionDocente = dto.IdAsignacionDocente,
            IdPeriodoPublicacion = null,
            IdCompetencia = null,
            IdResultadoAprendizaje = dto.IdResultadoAprendizaje,
            Nombre = dto.Nombre,
            Activa = true
        };

        _context.ActividadesCompetencias.Add(actividad);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            actividad.IdActividadCompetencia,
            actividad.IdResultadoAprendizaje,
            actividad.Nombre
        });
    }

    // Edita una actividad RA: cambia el nombre y, opcionalmente, la mueve a otro RA de la misma asignación
    // (sus notas se mueven con ella). Si cambia de RA y ya había notas RA calculadas, se recalculan.
    [HttpPut("ra/{id}")]
    public async Task<IActionResult> EditarActividadRA(int id, EditarActividadRADto dto)
    {
        var actividad = await _context.ActividadesCompetencias
            .Include(a => a.AsignacionDocente)
            .FirstOrDefaultAsync(a => a.IdActividadCompetencia == id && a.IdResultadoAprendizaje != null);

        if (actividad == null || !actividad.Activa)
            return NotFound("Actividad RA no encontrada.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(actividad.AsignacionDocente))
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre de la actividad es obligatorio.");

        var cambioDeRA = dto.IdResultadoAprendizaje.HasValue &&
                         dto.IdResultadoAprendizaje.Value != actividad.IdResultadoAprendizaje;

        if (cambioDeRA)
        {
            var raDestino = await _context.ResultadosAprendizaje
                .AnyAsync(r =>
                    r.IdResultadoAprendizaje == dto.IdResultadoAprendizaje &&
                    r.IdAsignacionDocente == actividad.IdAsignacionDocente &&
                    r.Activo);

            if (!raDestino)
                return BadRequest("El RA seleccionado no existe o no pertenece a esta asignación.");

            actividad.IdResultadoAprendizaje = dto.IdResultadoAprendizaje;
        }

        actividad.Nombre = dto.Nombre.Trim();
        await _context.SaveChangesAsync();

        var recalculo = cambioDeRA
            ? await _calculoRA.RecalcularSiYaCalculadoAsync(actividad.IdAsignacionDocente)
            : null;

        return Ok(new
        {
            mensaje = "Actividad actualizada correctamente.",
            actividad.IdActividadCompetencia,
            actividad.IdResultadoAprendizaje,
            actividad.Nombre,
            recalculado = recalculo?.Calculado ?? false,
            mensajeCalculo = recalculo?.Mensaje
        });
    }

    // Elimina una actividad RA. Si tiene notas, primero responde 409 con cuántas hay;
    // reenviar con ?confirmar=true borra la actividad junto con sus notas.
    // Si ya había notas RA calculadas, se recalculan para que no queden desactualizadas.
    [HttpDelete("ra/{id}")]
    public async Task<IActionResult> EliminarActividadRA(int id, [FromQuery] bool confirmar = false)
    {
        var actividad = await _context.ActividadesCompetencias
            .Include(a => a.AsignacionDocente)
            .FirstOrDefaultAsync(a => a.IdActividadCompetencia == id && a.IdResultadoAprendizaje != null);

        if (actividad == null)
            return NotFound("Actividad RA no encontrada.");

        if (User.IsInRole("Maestro") && !await EsDuenoAsync(actividad.AsignacionDocente))
            return Forbid();

        var notas = await _context.NotasCompetencias
            .Where(n => n.IdActividadCompetencia == id)
            .ToListAsync();

        if (notas.Count > 0 && !confirmar)
            return Conflict(new
            {
                mensaje = $"La actividad \"{actividad.Nombre}\" tiene {notas.Count} nota(s) registradas. " +
                          "Si la eliminas también se borrarán esas notas. Reenvíe con ?confirmar=true para eliminar.",
                notas = notas.Count
            });

        var idAsignacion = actividad.IdAsignacionDocente;

        _context.NotasCompetencias.RemoveRange(notas);
        _context.ActividadesCompetencias.Remove(actividad);
        await _context.SaveChangesAsync();

        var recalculo = notas.Count > 0
            ? await _calculoRA.RecalcularSiYaCalculadoAsync(idAsignacion)
            : null;

        return Ok(new
        {
            mensaje = "Actividad eliminada correctamente.",
            notasEliminadas = notas.Count,
            recalculado = recalculo?.Calculado ?? false,
            mensajeCalculo = recalculo?.Mensaje
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

public class CrearActividadRADto
{
    public int IdAsignacionDocente { get; set; }
    public int IdResultadoAprendizaje { get; set; }
    public string Nombre { get; set; } = "";
}

public class EditarActividadRADto
{
    public string Nombre { get; set; } = "";
    // Opcional: para mover la actividad a otro RA de la misma asignación
    public int? IdResultadoAprendizaje { get; set; }
}