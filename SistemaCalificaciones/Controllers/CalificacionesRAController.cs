using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;
using System.Security.Claims;

namespace SistemaCalificaciones.Controllers;

/// <summary>
/// Nota anual de las materias técnicas (por Resultados de Aprendizaje).
/// La nota de cada RA es la SUMA de las notas de sus actividades, topada al ValorMaximo del RA;
/// la nota anual es la suma de todos los RA (los RA suman 100). Es el mismo cálculo que usa
/// ModuloRA en el frontend y ReportesController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Maestro")]
public class CalificacionesRAController : ControllerBase
{
    private readonly AppDbContext _context;

    public CalificacionesRAController(AppDbContext context)
    {
        _context = context;
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

        var ras = await _context.ResultadosAprendizaje
            .Where(r => r.IdAsignacionDocente == idAsignacionDocente && r.Activo)
            .ToListAsync();

        var totalRA = ras.Sum(r => r.ValorMaximo);

        if (totalRA != 100)
            return BadRequest($"La suma de los RA debe ser exactamente 100. Actualmente suma {totalRA}.");

        var idsRA = ras.Select(r => r.IdResultadoAprendizaje).ToList();

        // Suma de notas por estudiante y RA (solo actividades activas).
        var sumas = await _context.NotasCompetencias
            .Where(n =>
                n.ActividadCompetencia.IdResultadoAprendizaje != null &&
                idsRA.Contains(n.ActividadCompetencia.IdResultadoAprendizaje.Value) &&
                n.ActividadCompetencia.Activa)
            .GroupBy(n => new { n.IdEstudiante, IdRA = n.ActividadCompetencia.IdResultadoAprendizaje!.Value })
            .Select(g => new { g.Key.IdEstudiante, g.Key.IdRA, Suma = g.Sum(n => n.Nota) })
            .ToListAsync();

        var valorMaximoPorRA = ras.ToDictionary(r => r.IdResultadoAprendizaje, r => r.ValorMaximo);

        var estudiantes = await _context.Inscripciones
            .Where(i =>
                i.IdCurso == asignacion.IdCurso &&
                i.IdAnioEscolar == asignacion.IdAnioEscolar &&
                i.Estado == "Activo")
            .Select(i => i.IdEstudiante)
            .Distinct()
            .ToListAsync();

        var existentes = await _context.CalificacionesRA
            .Where(c => c.IdAsignacionDocente == idAsignacionDocente)
            .ToListAsync();

        var ahora = DateTime.Now;
        var cambiosEnPublicadas = 0;

        foreach (var idEstudiante in estudiantes)
        {
            var notaFinal = Math.Round(
                sumas
                    .Where(s => s.IdEstudiante == idEstudiante)
                    .Sum(s => Math.Min(s.Suma, valorMaximoPorRA[s.IdRA])),
                2);

            var existente = existentes.FirstOrDefault(c => c.IdEstudiante == idEstudiante);

            if (existente == null)
            {
                _context.CalificacionesRA.Add(new CalificacionRA
                {
                    IdAsignacionDocente = idAsignacionDocente,
                    IdEstudiante = idEstudiante,
                    NotaFinal = notaFinal,
                    Publicada = false,
                    FechaCalculo = ahora
                });
            }
            else
            {
                if (existente.Publicada && existente.NotaFinal != notaFinal)
                    cambiosEnPublicadas++;

                existente.NotaFinal = notaFinal;
                existente.FechaCalculo = ahora;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = cambiosEnPublicadas > 0
                ? $"Calificaciones RA calculadas para {estudiantes.Count} estudiante(s). " +
                  $"{cambiosEnPublicadas} nota(s) ya publicada(s) cambiaron y se actualizaron."
                : $"Calificaciones RA calculadas para {estudiantes.Count} estudiante(s).",
            estudiantes = estudiantes.Count,
            cambiosEnPublicadas
        });
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
