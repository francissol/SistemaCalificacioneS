using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;

namespace SistemaCalificaciones.Services;

/// <summary>
/// Cálculo de la nota anual de las materias técnicas (por Resultados de Aprendizaje).
/// La nota de cada RA es la SUMA de las notas de sus actividades activas, topada al ValorMaximo del RA;
/// la nota anual es la suma de todos los RA (los RA suman 100). Es el mismo cálculo que usa
/// ModuloRA en el frontend y ReportesController.
/// Lo usan: calcular, guardar-y-calcular, y editar/eliminar actividades RA (para no dejar notas viejas).
/// </summary>
public class CalculoRAService
{
    private readonly AppDbContext _context;

    public CalculoRAService(AppDbContext context)
    {
        _context = context;
    }

    public record ResultadoCalculoRA(bool Calculado, string Mensaje, int Estudiantes, int CambiosEnPublicadas);

    // Calcula y guarda (SaveChanges incluido) las CalificacionesRA de la asignación.
    // Si los RA no suman 100 no calcula y devuelve Calculado = false con el motivo.
    public async Task<ResultadoCalculoRA> CalcularAsync(int idAsignacionDocente)
    {
        var asignacion = await _context.AsignacionesDocentes
            .FirstAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        var ras = await _context.ResultadosAprendizaje
            .Where(r => r.IdAsignacionDocente == idAsignacionDocente && r.Activo)
            .ToListAsync();

        var totalRA = ras.Sum(r => r.ValorMaximo);

        if (totalRA != 100)
            return new ResultadoCalculoRA(false,
                $"La suma de los RA debe ser exactamente 100. Actualmente suma {totalRA}.", 0, 0);

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

        var mensaje = cambiosEnPublicadas > 0
            ? $"Calificaciones RA calculadas para {estudiantes.Count} estudiante(s). " +
              $"{cambiosEnPublicadas} nota(s) ya publicada(s) cambiaron y se actualizaron."
            : $"Calificaciones RA calculadas para {estudiantes.Count} estudiante(s).";

        return new ResultadoCalculoRA(true, mensaje, estudiantes.Count, cambiosEnPublicadas);
    }

    // Recalcula solo si la asignación ya tenía notas RA calculadas; así editar o eliminar
    // una actividad no deja notas calculadas (o publicadas) desactualizadas.
    public async Task<ResultadoCalculoRA?> RecalcularSiYaCalculadoAsync(int idAsignacionDocente)
    {
        var yaCalculado = await _context.CalificacionesRA
            .AnyAsync(c => c.IdAsignacionDocente == idAsignacionDocente);

        return yaCalculado ? await CalcularAsync(idAsignacionDocente) : null;
    }
}
