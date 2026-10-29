using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalificacionesCompetenciasPeriodoController : ControllerBase
{
    private const string RolesCaptura = "Administrador,Maestro,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico";

    private readonly AppDbContext _context;

    public CalificacionesCompetenciasPeriodoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("captura-directa")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> ObtenerCapturaDirecta(int idAsignacionDocente, int idPeriodoPublicacion)
    {
        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Curso).ThenInclude(c => c.Grado).ThenInclude(g => g.Nivel)
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return NotFound("No se encontró la asignación docente.");

        if (asignacion.Materia.EsTecnica)
            return BadRequest("Las materias técnicas deben continuar usando actividades y resultados de aprendizaje.");

        if (!asignacion.Curso.Grado.Nivel.UsaCompetencias)
            return BadRequest("Este nivel no está configurado para trabajar con competencias.");

        var periodo = await _context.PeriodosPublicacion
            .FirstOrDefaultAsync(p => p.IdPeriodoPublicacion == idPeriodoPublicacion);

        if (periodo == null)
            return NotFound("No se encontró el período.");

        if (periodo.IdAnioEscolar != asignacion.IdAnioEscolar)
            return BadRequest("El período no pertenece al año escolar de esta asignación.");

        var competencias = await _context.CompetenciasGradoMateria
            .Where(x => x.IdGrado == asignacion.Curso.IdGrado && x.IdMateria == asignacion.IdMateria)
            .Select(x => new { x.IdCompetencia, x.Competencia.Codigo, x.Competencia.Nombre })
            .OrderBy(x => x.Codigo)
            .ToListAsync();

        if (competencias.Count == 0)
            return BadRequest("No hay competencias configuradas para esta materia y grado.");

        var estudiantes = await _context.Inscripciones
            .Where(i => i.IdCurso == asignacion.IdCurso &&
                        i.IdAnioEscolar == asignacion.IdAnioEscolar &&
                        i.Estado == "Activo" && i.Estudiante.Activo)
            .Select(i => new
            {
                i.Estudiante.IdEstudiante,
                i.Estudiante.Matricula,
                i.Estudiante.Nombres,
                i.Estudiante.Apellidos
            })
            .OrderBy(e => e.Apellidos).ThenBy(e => e.Nombres)
            .ToListAsync();

        var notas = await _context.CalificacionesCompetenciasPeriodo
            .Where(c => c.IdAsignacionDocente == idAsignacionDocente &&
                        c.IdPeriodoPublicacion == idPeriodoPublicacion)
            .Select(c => new
            {
                c.IdEstudiante,
                c.IdCompetencia,
                Nota = (decimal?)c.Promedio,
                c.Publicada,
                c.FechaPublicacion,
                c.FechaRegistro
            })
            .ToListAsync();

        return Ok(new
        {
            asignacion.IdAsignacionDocente,
            asignacion.IdMateria,
            Materia = asignacion.Materia.Nombre,
            Grado = asignacion.Curso.Grado.Nombre,
            Nivel = asignacion.Curso.Grado.Nivel.Nombre,
            periodo.IdPeriodoPublicacion,
            Periodo = periodo.Nombre,
            periodo.Activo,
            periodo.FechaCierre,
            PuedeEditar = periodo.Activo && DateTime.Today <= periodo.FechaCierre.Date,
            Estudiantes = estudiantes,
            Competencias = competencias,
            Calificaciones = notas
        });
    }

    // Guarda o actualiza notas directas. Una nota nula elimina la nota borrador existente.
    [HttpPut("guardar-directas")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> GuardarNotasDirectas([FromBody] GuardarNotasDirectasDto dto)
    {
        if (dto.IdAsignacionDocente <= 0 || dto.IdPeriodoPublicacion <= 0)
            return BadRequest("Debes indicar una asignación docente y un período válidos.");

        if (dto.Notas == null || dto.Notas.Count == 0)
            return BadRequest("Debes enviar al menos una nota para guardar.");

        if (dto.Notas.GroupBy(n => new { n.IdEstudiante, n.IdCompetencia }).Any(g => g.Count() > 1))
            return BadRequest("La solicitud contiene notas duplicadas para un estudiante y una competencia.");

        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Curso).ThenInclude(c => c.Grado).ThenInclude(g => g.Nivel)
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == dto.IdAsignacionDocente);

        if (asignacion == null)
            return NotFound("No se encontró la asignación docente.");

        if (asignacion.Materia.EsTecnica)
            return BadRequest("No se permiten notas directas en materias técnicas; utiliza el flujo de actividades y resultados de aprendizaje.");

        if (!asignacion.Curso.Grado.Nivel.UsaCompetencias)
            return BadRequest("Este nivel no está configurado para trabajar con competencias.");

        var periodo = await _context.PeriodosPublicacion
            .FirstOrDefaultAsync(p => p.IdPeriodoPublicacion == dto.IdPeriodoPublicacion);

        if (periodo == null)
            return NotFound("No se encontró el período.");

        if (periodo.IdAnioEscolar != asignacion.IdAnioEscolar)
            return BadRequest("El período no pertenece al año escolar de esta asignación.");

        if (!periodo.Activo || DateTime.Today > periodo.FechaCierre.Date)
            return BadRequest("El período está inactivo o ya cerró. No se pueden modificar sus notas.");

        var existentes = await _context.CalificacionesCompetenciasPeriodo
            .Where(c => c.IdAsignacionDocente == dto.IdAsignacionDocente &&
                        c.IdPeriodoPublicacion == dto.IdPeriodoPublicacion)
            .ToListAsync();

     

        var idsEstudiantes = dto.Notas.Select(n => n.IdEstudiante).Distinct().ToList();
        var idsCompetencias = dto.Notas.Select(n => n.IdCompetencia).Distinct().ToList();

        var estudiantesValidos = await _context.Inscripciones
            .Where(i => idsEstudiantes.Contains(i.IdEstudiante) &&
                        i.IdCurso == asignacion.IdCurso &&
                        i.IdAnioEscolar == asignacion.IdAnioEscolar &&
                        i.Estado == "Activo" && i.Estudiante.Activo)
            .Select(i => i.IdEstudiante)
            .Distinct()
            .ToListAsync();

        var competenciasValidas = await _context.CompetenciasGradoMateria
            .Where(c => c.IdGrado == asignacion.Curso.IdGrado &&
                        c.IdMateria == asignacion.IdMateria &&
                        idsCompetencias.Contains(c.IdCompetencia))
            .Select(c => c.IdCompetencia)
            .Distinct()
            .ToListAsync();

        if (estudiantesValidos.Count != idsEstudiantes.Count)
            return BadRequest("Una o más personas no están inscritas activamente en el curso de esta asignación.");

        if (competenciasValidas.Count != idsCompetencias.Count)
            return BadRequest("Una o más competencias no están asignadas a la materia y grado de esta asignación.");

        foreach (var item in dto.Notas)
        {
            if (item.Nota.HasValue && (item.Nota.Value < 0 || item.Nota.Value > 100))
                return BadRequest("Las notas deben estar entre 0 y 100.");
        }

        foreach (var item in dto.Notas)
        {
            var actual = existentes.FirstOrDefault(c =>
                c.IdEstudiante == item.IdEstudiante && c.IdCompetencia == item.IdCompetencia);

            if (!item.Nota.HasValue)
            {
                if (actual != null) _context.CalificacionesCompetenciasPeriodo.Remove(actual);
                continue;
            }

            if (actual == null)
            {
                _context.CalificacionesCompetenciasPeriodo.Add(new CalificacionCompetenciaPeriodo
                {
                    IdEstudiante = item.IdEstudiante,
                    IdAsignacionDocente = dto.IdAsignacionDocente,
                    IdPeriodoPublicacion = dto.IdPeriodoPublicacion,
                    IdCompetencia = item.IdCompetencia,
                    Promedio = item.Nota.Value,
                    Publicada = false,
                    FechaRegistro = DateTime.Now
                });
            }
            else
            {
                actual.Promedio = item.Nota.Value;
                actual.FechaRegistro = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Notas directas guardadas correctamente.", cantidad = dto.Notas.Count });
    }

    public class GuardarNotasDirectasDto
    {
        public int IdAsignacionDocente { get; set; }
        public int IdPeriodoPublicacion { get; set; }
        public List<NotaDirectaDto> Notas { get; set; } = new();
    }

    public class NotaDirectaDto
    {
        public int IdEstudiante { get; set; }
        public int IdCompetencia { get; set; }
        public decimal? Nota { get; set; }
    }

    [HttpPost("calcular")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> Calcular(int idAsignacionDocente, int idPeriodoPublicacion)
    {
        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Curso)
                .ThenInclude(c => c.Grado)
                    .ThenInclude(g => g.Nivel)
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a => a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return BadRequest("Asignación docente no encontrada.");

        if (asignacion.Materia.EsTecnica)
            return BadRequest("Las materias técnicas usan Resultados de Aprendizaje.");

        if (asignacion.Curso.Grado.Nivel.UsaCompetencias)
            return BadRequest("Este nivel utiliza captura directa de notas por competencia. Usa el endpoint guardar-directas en lugar de calcular promedios de actividades.");

        var estudiantes = await _context.Inscripciones
            .Where(i =>
                i.IdCurso == asignacion.IdCurso &&
                i.IdAnioEscolar == asignacion.IdAnioEscolar &&
                i.Estado == "Activo")
            .Select(i => i.Estudiante)
            .ToListAsync();

        var competencias = await _context.CompetenciasGradoMateria
            .Where(cgm =>
                cgm.IdGrado == asignacion.Curso.IdGrado &&
                cgm.IdMateria == asignacion.IdMateria)
            .Select(cgm => cgm.Competencia)
            .ToListAsync();

        if (!competencias.Any())
            return BadRequest("No hay competencias asignadas a esta materia y grado.");

        foreach (var estudiante in estudiantes)
        {
            foreach (var competencia in competencias)
            {
                var actividades = await _context.ActividadesCompetencias
                    .Where(a =>
                        a.IdAsignacionDocente == idAsignacionDocente &&
                        a.IdPeriodoPublicacion == idPeriodoPublicacion &&
                        a.IdCompetencia == competencia.IdCompetencia &&
                        a.IdResultadoAprendizaje == null &&
                        a.Activa)
                    .ToListAsync();

                var idsActividades = actividades
                    .Select(a => a.IdActividadCompetencia)
                    .ToList();

                var notas = await _context.NotasCompetencias
                    .Where(n =>
                        idsActividades.Contains(n.IdActividadCompetencia) &&
                        n.IdEstudiante == estudiante.IdEstudiante)
                    .ToListAsync();

                if (!notas.Any())
                    continue;

                var promedio = Math.Round(notas.Average(n => n.Nota), 2);

                var calificacion = await _context.CalificacionesCompetenciasPeriodo
                    .FirstOrDefaultAsync(c =>
                        c.IdEstudiante == estudiante.IdEstudiante &&
                        c.IdAsignacionDocente == idAsignacionDocente &&
                        c.IdPeriodoPublicacion == idPeriodoPublicacion &&
                        c.IdCompetencia == competencia.IdCompetencia);

                if (calificacion == null)
                {
                    calificacion = new CalificacionCompetenciaPeriodo
                    {
                        IdEstudiante = estudiante.IdEstudiante,
                        IdAsignacionDocente = idAsignacionDocente,
                        IdPeriodoPublicacion = idPeriodoPublicacion,
                        IdCompetencia = competencia.IdCompetencia,
                        Promedio = promedio,
                        Publicada = false,
                        FechaRegistro = DateTime.Now
                    };

                    _context.CalificacionesCompetenciasPeriodo.Add(calificacion);
                }
                else
                {
                    calificacion.Promedio = promedio;
                    calificacion.FechaRegistro = DateTime.Now;
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok("Calificaciones por competencias calculadas correctamente.");
    }


    [HttpPut("publicar")]
    [Authorize(Roles = RolesCaptura)]
    public async Task<IActionResult> Publicar(
        int idAsignacionDocente,
        int idPeriodoPublicacion)
    {
        if (idAsignacionDocente <= 0 || idPeriodoPublicacion <= 0)
            return BadRequest("Debes indicar una asignación y un período válidos.");

        var asignacion = await _context.AsignacionesDocentes
            .Include(a => a.Curso)
                .ThenInclude(c => c.Grado)
                    .ThenInclude(g => g.Nivel)
            .Include(a => a.Materia)
            .FirstOrDefaultAsync(a =>
                a.IdAsignacionDocente == idAsignacionDocente);

        if (asignacion == null)
            return NotFound("No se encontró la asignación docente.");

        if (asignacion.Materia.EsTecnica)
            return BadRequest(
                "Las materias técnicas deben publicarse mediante el módulo de resultados de aprendizaje (RA).");

        var periodo = await _context.PeriodosPublicacion
            .FirstOrDefaultAsync(p =>
                p.IdPeriodoPublicacion == idPeriodoPublicacion);

        if (periodo == null)
            return NotFound("No se encontró el período.");

        if (periodo.IdAnioEscolar != asignacion.IdAnioEscolar)
            return BadRequest(
                "El período no pertenece al año escolar de esta asignación.");

        if (!periodo.Activo || DateTime.Today > periodo.FechaCierre.Date)
            return BadRequest(
                "El período está inactivo o ya cerró. No se pueden publicar las calificaciones.");

        var calificaciones = await _context.CalificacionesCompetenciasPeriodo
            .Where(c =>
                c.IdAsignacionDocente == idAsignacionDocente &&
                c.IdPeriodoPublicacion == idPeriodoPublicacion)
            .ToListAsync();

        if (!calificaciones.Any())
            return BadRequest(
                "No hay calificaciones guardadas para publicar. Primero guarda las notas directas.");

        foreach (var calificacion in calificaciones)
        {
            calificacion.Publicada = true;
            calificacion.FechaPublicacion = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Calificaciones publicadas correctamente.",
            cantidad = calificaciones.Count
        });
    }


    [HttpGet("estudiante/{idEstudiante}")]
    [Authorize(Roles = "Administrador,Maestro,Estudiante")]
    public async Task<IActionResult> GetPorEstudiante(int idEstudiante)
    {
        var calificaciones = await _context.CalificacionesCompetenciasPeriodo
            .Include(c => c.AsignacionDocente)
                .ThenInclude(a => a.Materia)
            .Include(c => c.PeriodoPublicacion)
            .Include(c => c.Competencia)
            .Where(c => c.IdEstudiante == idEstudiante && c.Publicada)
            .OrderBy(c => c.AsignacionDocente.Materia.Nombre)
            .ThenBy(c => c.Competencia.Codigo)
            .Select(c => new
            {
                c.IdCalificacionCompetenciaPeriodo,
                c.IdAsignacionDocente,
                c.IdPeriodoPublicacion,
                c.IdCompetencia,
                Materia = c.AsignacionDocente.Materia.Nombre,
                Periodo = c.PeriodoPublicacion.Nombre,
                Competencia = c.Competencia.Codigo,
                NombreCompetencia = c.Competencia.Nombre,
                c.Promedio,
                c.FechaPublicacion
            })
            .ToListAsync();

        return Ok(calificaciones);
    }
}