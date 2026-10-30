using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.DTOs.Estudiantes;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Helpers;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico")]
public class EstudiantesController : BaseController
{
    private readonly AppDbContext _context;

    public EstudiantesController(AppDbContext context)
    {
        _context = context;
    }

  

    [HttpGet]
     public async Task<IActionResult> Get()
    {
        var query = _context.Estudiantes
            .Include(e => e.Inscripciones)
                .ThenInclude(i => i.Curso)
                    .ThenInclude(c => c.Grado)
                        .ThenInclude(g => g.Nivel)
            .AsQueryable();

        if (!EsAdministrador)
        {
            query = query.Where(e => e.CentroId == IdCentro);
        }

        var estudiantes = await query
            .OrderBy(e => e.Nombres)
            .Select(e => new
            {
                e.IdEstudiante,
                e.Matricula,
                e.Nombres,
                e.Apellidos,
                e.Telefono,
                e.Correo,
                e.Activo,

                e.CentroId,
                Centro = e.Centro != null ? e.Centro.Nombre : null,

                Grado = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Grado.Nombre)
                    .FirstOrDefault(),

                CursoActual = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Nombre)
                    .FirstOrDefault(),

                Nivel = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Grado.Nivel.Nombre)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(estudiantes);
    }

    // ============================================================
    // LISTADO PAGINADO
    // ============================================================

    [HttpGet("paginado")]
    public async Task<IActionResult> GetPaginado(
        int pagina = 1,
        int tamanoPagina = 20,
        string? busqueda = null,
        string? centro = null,
        string? nivel = null,
        string? grado = null,
        string? curso = null,
        string? estado = null)
    {
        var query = _context.Estudiantes.AsQueryable();

        if (!EsAdministrador)
        {
            query = query.Where(e => e.CentroId == IdCentro);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();

            query = query.Where(e =>
                e.Nombres.Contains(texto) ||
                e.Apellidos.Contains(texto) ||
                (e.Nombres + " " + e.Apellidos).Contains(texto) ||
                e.Matricula.Contains(texto) ||
                (e.Correo != null && e.Correo.Contains(texto)));
        }

        if (estado == "Activo")
            query = query.Where(e => e.Activo);
        else if (estado == "Inactivo")
            query = query.Where(e => !e.Activo);

        var listado = query
            .OrderBy(e => e.Nombres)
            .ThenBy(e => e.Apellidos)
            .ThenBy(e => e.IdEstudiante)
            .Select(e => new
            {
                e.IdEstudiante,
                e.Matricula,
                e.Nombres,
                e.Apellidos,
                e.Telefono,
                e.Correo,
                e.Activo,

                e.CentroId,
                Centro = e.Centro != null ? e.Centro.Nombre : null,

                Grado = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Grado.Nombre)
                    .FirstOrDefault(),

                CursoActual = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Nombre)
                    .FirstOrDefault(),

                Nivel = e.Inscripciones
                    .Where(i => i.Estado == "Activo")
                    .OrderByDescending(i => i.IdInscripcion)
                    .Select(i => i.Curso.Grado.Nivel.Nombre)
                    .FirstOrDefault()
            });

        if (!string.IsNullOrWhiteSpace(centro))
            listado = listado.Where(e => e.Centro == centro);

        if (!string.IsNullOrWhiteSpace(nivel))
            listado = listado.Where(e => e.Nivel == nivel);

        if (!string.IsNullOrWhiteSpace(grado))
            listado = listado.Where(e => e.Grado == grado);

        if (!string.IsNullOrWhiteSpace(curso))
            listado = listado.Where(e => e.CursoActual == curso);

        return Ok(await listado.PaginarAsync(pagina, tamanoPagina));
    }

    // ============================================================
    // OPCIONES DE FILTROS
    // ============================================================

    [HttpGet("filtros")]
    public async Task<IActionResult> GetFiltros()
    {
        var estudiantes = _context.Estudiantes.AsQueryable();

        if (!EsAdministrador)
        {
            estudiantes = estudiantes.Where(e => e.CentroId == IdCentro);
        }

        var centros = await estudiantes
            .Where(e => e.Centro != null)
            .Select(e => e.Centro!.Nombre)
            .Distinct()
            .ToListAsync();

        // Combinaciones existentes para que cada filtro dependa del anterior
        var combinaciones = await estudiantes
            .SelectMany(e => e.Inscripciones
                .Where(i => i.Estado == "Activo")
                .Select(i => new
                {
                    Centro = e.Centro != null ? e.Centro.Nombre : null,
                    Nivel = i.Curso.Grado.Nivel.Nombre,
                    Grado = i.Curso.Grado.Nombre,
                    Curso = i.Curso.Nombre
                }))
            .Distinct()
            .ToListAsync();

        return Ok(new { centros, combinaciones });
    }

    // ============================================================
    // OBTENER ESTUDIANTE
    // ============================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var nivelCoordinador = NivelHelper.ObtenerNivelPorRol(User);

        var estudiante = await _context.Estudiantes
            .Include(e => e.PadreEstudiantes)
                .ThenInclude(pe => pe.Padre)
            .Include(e => e.Inscripciones)
                .ThenInclude(i => i.Curso)
                    .ThenInclude(c => c.Grado)
                        .ThenInclude(g => g.Nivel)
            .FirstOrDefaultAsync(e => e.IdEstudiante == id);

        if (estudiante == null)
            return NotFound("Estudiante no encontrado.");

        if (nivelCoordinador != null)
        {
            var perteneceNivel = estudiante.Inscripciones.Any(i =>
                i.Estado == "Activo" &&
                i.Curso.Grado.Nivel.Nombre == nivelCoordinador);

            if (!perteneceNivel)
                return Forbid();
        }

        if (!EsAdministrador)
        {
            var perteneceCentro = estudiante.Inscripciones.Any(i =>
                i.Estado == "Activo" &&
                i.Curso.CentroId == IdCentro);

            if (!perteneceCentro)
                return Forbid();
        }

        return Ok(estudiante);
    }

    // ============================================================
    // CREAR ESTUDIANTE
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Crear(CrearEstudianteDto dto)
    {
        using var transaction =
            await _context.Database.BeginTransactionAsync();

        var existeMatricula = await _context.Estudiantes
            .AnyAsync(e => e.Matricula == dto.Matricula);

        if (existeMatricula)
            return BadRequest(
                "Ya existe un estudiante con esa matrícula.");

        var nivelCoordinador =
            NivelHelper.ObtenerNivelPorRol(User);

        var curso = await _context.Cursos
            .Include(c => c.Grado)
                .ThenInclude(g => g.Nivel)
            .FirstOrDefaultAsync(
                c => c.IdCurso == dto.IdCurso && c.Activo);

        if (curso == null)
            return BadRequest(
                "El curso no existe o está inactivo.");

        if (!EsAdministrador && curso.CentroId != IdCentro)
            return Forbid();

        if (nivelCoordinador != null &&
            curso.Grado.Nivel.Nombre != nivelCoordinador)
            return Forbid();

        var anioExiste = await _context.AniosEscolares
            .AnyAsync(a =>
                a.IdAnioEscolar == dto.IdAnioEscolar &&
                !a.Cerrado);

        if (!anioExiste)
            return BadRequest(
                "El año escolar no existe o está cerrado.");

        var estudiante = new Estudiante
        {
            CentroId = curso.CentroId,
            Matricula = dto.Matricula,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            FechaIngreso = dto.FechaIngreso,
            Activo = true
        };

        _context.Estudiantes.Add(estudiante);

        await _context.SaveChangesAsync();

        var inscripcion = new Inscripcion
        {
            IdEstudiante = estudiante.IdEstudiante,
            IdCurso = dto.IdCurso,
            IdAnioEscolar = dto.IdAnioEscolar,
            Estado = "Activo"
        };

        _context.Inscripciones.Add(inscripcion);

        Padre? padre = null;

        if (!string.IsNullOrWhiteSpace(dto.NombrePadre) &&
            !string.IsNullOrWhiteSpace(dto.ApellidoPadre))
        {
            padre = await _context.Padres
                .FirstOrDefaultAsync(p =>
                    p.Nombres == dto.NombrePadre &&
                    p.Apellidos == dto.ApellidoPadre &&
                    p.Telefono == dto.TelefonoPadre);

            if (padre == null)
            {
                padre = new Padre
                {
                    Nombres = dto.NombrePadre,
                    Apellidos = dto.ApellidoPadre,
                    Telefono = dto.TelefonoPadre,
                    Correo = dto.CorreoPadre,
                    Activo = true
                };

                _context.Padres.Add(padre);

                await _context.SaveChangesAsync();
            }

            var relacionExiste =
                await _context.PadreEstudiantes.AnyAsync(pe =>
                    pe.IdPadre == padre.IdPadre &&
                    pe.IdEstudiante == estudiante.IdEstudiante);

            if (!relacionExiste)
            {
                _context.PadreEstudiantes.Add(
                    new PadreEstudiante
                    {
                        IdPadre = padre.IdPadre,
                        IdEstudiante = estudiante.IdEstudiante,
                        Parentesco = dto.Parentesco,
                        ResponsableAcademico = true
                    });
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            mensaje = "Estudiante creado correctamente.",
            estudiante.IdEstudiante,
            PadreCreadoORelacionado = padre != null
        });
    }

    // ============================================================
    // ACTUALIZAR DATOS DE CONTACTO
    // ============================================================

    [HttpPut("{id}/datos-contacto")]
    public async Task<IActionResult> ActualizarDatosContacto(
        int id,
        ActualizarDatosEstudianteDto dto)
    {
        var nivelCoordinador =
            NivelHelper.ObtenerNivelPorRol(User);

        var estudiante = await _context.Estudiantes
            .Include(e => e.Inscripciones)
                .ThenInclude(i => i.Curso)
                    .ThenInclude(c => c.Grado)
                        .ThenInclude(g => g.Nivel)
            .FirstOrDefaultAsync(e => e.IdEstudiante == id);

        if (estudiante == null)
            return NotFound("Estudiante no encontrado.");

        if (nivelCoordinador != null)
        {
            var perteneceNivel = estudiante.Inscripciones.Any(i =>
                i.Estado == "Activo" &&
                i.Curso.Grado.Nivel.Nombre == nivelCoordinador);

            if (!perteneceNivel)
                return Forbid();
        }

        estudiante.Telefono = dto.Telefono;
        estudiante.Correo = dto.Correo;
        estudiante.Direccion = dto.Direccion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Datos de contacto actualizados correctamente.",
            estudiante.IdEstudiante,
            estudiante.Telefono,
            estudiante.Correo,
            estudiante.Direccion
        });
    }

    // ============================================================
    // CAMBIAR ESTADO
    // ============================================================

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var nivelCoordinador =
            NivelHelper.ObtenerNivelPorRol(User);

        var estudiante = await _context.Estudiantes
            .Include(e => e.Inscripciones)
                .ThenInclude(i => i.Curso)
                    .ThenInclude(c => c.Grado)
                        .ThenInclude(g => g.Nivel)
            .FirstOrDefaultAsync(e => e.IdEstudiante == id);

        if (estudiante == null)
            return NotFound("Estudiante no encontrado.");

        if (nivelCoordinador != null)
        {
            var perteneceNivel = estudiante.Inscripciones.Any(i =>
                i.Estado == "Activo" &&
                i.Curso.Grado.Nivel.Nombre == nivelCoordinador);

            if (!perteneceNivel)
                return Forbid();
        }

        estudiante.Activo = !estudiante.Activo;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado del estudiante actualizado.",
            estudiante.IdEstudiante,
            estudiante.Activo
        });
    }
}