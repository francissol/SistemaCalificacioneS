
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.DTOs.Competencias;
using SistemaCalificaciones.Models;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador, Maestro")]
public class CompetenciasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CompetenciasController(AppDbContext context)
    {
        _context = context;
    }

    // Consultar el catálogo de competencias.
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var competencias = await _context.Competencias
            .AsNoTracking()
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        return Ok(competencias);
    }

    // Crear una competencia en el catálogo.
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearCompetenciaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) ||
            string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(
                "El código y el nombre son obligatorios.");
        }

        var codigo = dto.Codigo.Trim();
        var nombre = dto.Nombre.Trim();

        var existe = await _context.Competencias
            .AnyAsync(c =>
                c.Codigo == codigo ||
                c.Nombre == nombre);

        if (existe)
        {
            return BadRequest(
                "Ya existe una competencia con ese código o nombre.");
        }

        var competencia = new Competencia
        {
            Codigo = codigo,
            Nombre = nombre,
            Activa = true
        };

        _context.Competencias.Add(competencia);
        await _context.SaveChangesAsync();

        return Ok(competencia);
    }

    // Editar una competencia.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] CrearCompetenciaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) ||
            string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(
                "El código y el nombre son obligatorios.");
        }

        var competencia = await _context.Competencias
            .FindAsync(id);

        if (competencia == null)
            return NotFound("Competencia no encontrada.");

        var codigo = dto.Codigo.Trim();
        var nombre = dto.Nombre.Trim();

        var duplicada = await _context.Competencias
            .AnyAsync(c =>
                c.IdCompetencia != id &&
                (c.Codigo == codigo || c.Nombre == nombre));

        if (duplicada)
        {
            return BadRequest(
                "Otra competencia ya utiliza ese código o nombre.");
        }

        competencia.Codigo = codigo;
        competencia.Nombre = nombre;

        await _context.SaveChangesAsync();

        return Ok(competencia);
    }

    // Activar o desactivar una competencia.
    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var competencia = await _context.Competencias
            .FindAsync(id);

        if (competencia == null)
            return NotFound("Competencia no encontrada.");

        competencia.Activa = !competencia.Activa;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            competencia.IdCompetencia,
            competencia.Activa
        });
    }

    // Consultar los grados y materias válidos
    // para asignar competencias.
    [HttpGet("opciones-asignacion")]
    public async Task<IActionResult> ObtenerOpcionesAsignacion()
    {
        var opciones = await _context.GradoMaterias
            .AsNoTracking()
            .Where(gm =>
                gm.Grado.Nivel.UsaCompetencias &&
                gm.Materia.Activa &&
                !gm.Materia.EsTecnica)
            .Select(gm => new
            {
                gm.IdGradoMateria,
                gm.IdGrado,
                Grado = gm.Grado.Nombre,
                gm.Grado.IdNivel,
                Nivel = gm.Grado.Nivel.Nombre,
                gm.IdMateria,
                Materia = gm.Materia.Nombre
            })
            .Distinct()
            .OrderBy(x => x.Nivel)
            .ThenBy(x => x.Grado)
            .ThenBy(x => x.Materia)
            .ToListAsync();

        return Ok(opciones);
    }

    // Asignar una competencia a un grado y una materia.
    [HttpPost("asignaciones")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Asignar(
        [FromBody] AsignarCompetenciaGradoMateriaDto dto)
    {
        var competencia = await _context.Competencias
            .FirstOrDefaultAsync(c =>
                c.IdCompetencia == dto.IdCompetencia &&
                c.Activa);

        if (competencia == null)
        {
            return BadRequest(
                "La competencia no existe o está desactivada.");
        }

        var gradoMateria = await _context.GradoMaterias
        .FirstOrDefaultAsync(gm =>
            gm.IdGrado == dto.IdGrado &&
            gm.IdMateria == dto.IdMateria &&
            gm.Grado.Nivel.UsaCompetencias &&
            gm.Materia.Activa &&
            !gm.Materia.EsTecnica);

        if (gradoMateria == null)
        {
            return BadRequest(
                "El grado y la materia no existen, no están relacionados " +
                "o no permiten la captura directa por competencias.");
        }

        var existe = await _context.CompetenciasGradoMateria
            .AnyAsync(cgm =>
                cgm.IdCompetencia == dto.IdCompetencia &&
                cgm.IdGrado == gradoMateria.IdGrado &&
                cgm.IdMateria == gradoMateria.IdMateria);

        if (existe)
        {
            return BadRequest(
                "Esta competencia ya está asignada a ese grado y materia.");
        }

        var asignacion = new CompetenciaGradoMateria
        {
            IdCompetencia = dto.IdCompetencia,
            IdGrado = gradoMateria.IdGrado,
            IdMateria = gradoMateria.IdMateria
        };

        _context.CompetenciasGradoMateria.Add(asignacion);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Competencia asignada correctamente.",
            asignacion.IdCompetenciaGradoMateria,
            asignacion.IdCompetencia,
            asignacion.IdGrado,
            asignacion.IdMateria
        });
    }

    // Consultar las asignaciones existentes.
    [HttpGet("asignaciones")]
    public async Task<IActionResult> ObtenerAsignaciones()
    {
        var asignaciones = await _context.CompetenciasGradoMateria
            .AsNoTracking()
            .Where(cgm =>
                cgm.Competencia.Activa &&
                cgm.Grado.Nivel.UsaCompetencias &&
                cgm.Materia.Activa &&
                !cgm.Materia.EsTecnica)
            .Select(cgm => new
            {
                cgm.IdCompetenciaGradoMateria,
                cgm.IdCompetencia,
                cgm.Competencia.Codigo,
                Competencia = cgm.Competencia.Nombre,
                cgm.IdGrado,
                Grado = cgm.Grado.Nombre,
                Nivel = cgm.Grado.Nivel.Nombre,
                cgm.IdMateria,
                Materia = cgm.Materia.Nombre
            })
            .OrderBy(x => x.Nivel)
            .ThenBy(x => x.Grado)
            .ThenBy(x => x.Materia)
            .ThenBy(x => x.Codigo)
            .ToListAsync();

        return Ok(asignaciones);
    }

    // Quitar una asignación sin eliminar la competencia del catálogo.
    [HttpDelete("asignaciones/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarAsignacion(int id)
    {
        var asignacion = await _context.CompetenciasGradoMateria
            .FindAsync(id);

        if (asignacion == null)
            return NotFound("Asignación no encontrada.");

        _context.CompetenciasGradoMateria.Remove(asignacion);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
