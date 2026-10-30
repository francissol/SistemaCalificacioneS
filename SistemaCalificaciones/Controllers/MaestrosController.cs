using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.DTOs.Maestros;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;
using SistemaCalificaciones.Helpers;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico")]
public class MaestrosController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UsuarioGeneratorService _usuarioGenerator;

    public MaestrosController(AppDbContext context, UsuarioGeneratorService usuarioGenerator)
    {
        _context = context;
        _usuarioGenerator = usuarioGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = _context.Maestros
     .Include(m => m.Usuario)
     .Include(m => m.Centro)
     .AsQueryable();

        if (!EsAdministrador)
        {
            query = query.Where(m => m.CentroId == IdCentro);
        }

        var maestros = await query
                    .OrderBy(m => m.Nombres)
            .Select(m => new
            {
                m.IdMaestro,
                m.CentroId,
                Centro = m.Centro.Nombre,
                m.CodigoEmpleado,
                m.Nombres,
                m.Apellidos,
                m.Cedula,
                m.Telefono,
                m.Correo,
                m.Direccion,
                m.Especialidad,
                m.FechaIngreso,
                m.Activo,
                Usuario = m.Usuario != null ? m.Usuario.NombreUsuario : null
            })
            .ToListAsync();

        return Ok(maestros);
    }

    [HttpGet("paginado")]
    public async Task<IActionResult> GetPaginado(
        int pagina = 1,
        int tamanoPagina = 20,
        string? busqueda = null,
        string? centro = null,
        string? nivel = null,
        string? curso = null,
        string? materia = null,
        string? especialidad = null,
        string? estado = null)
    {
        var query = _context.Maestros.AsQueryable();

        if (!EsAdministrador)
        {
            query = query.Where(m => m.CentroId == IdCentro);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();

            query = query.Where(m =>
                m.Nombres.Contains(texto) ||
                m.Apellidos.Contains(texto) ||
                (m.Nombres + " " + m.Apellidos).Contains(texto) ||
                (m.Usuario != null && m.Usuario.NombreUsuario.Contains(texto)) ||
                (m.Cedula != null && m.Cedula.Contains(texto)) ||
                (m.CodigoEmpleado != null && m.CodigoEmpleado.Contains(texto)) ||
                (m.Correo != null && m.Correo.Contains(texto)));
        }

        if (!string.IsNullOrWhiteSpace(centro))
            query = query.Where(m => m.Centro != null && m.Centro.Nombre == centro);

        if (!string.IsNullOrWhiteSpace(especialidad))
            query = query.Where(m => m.Especialidad == especialidad);

        if (estado == "Activo")
            query = query.Where(m => m.Activo);
        else if (estado == "Inactivo")
            query = query.Where(m => !m.Activo);

        // Nivel, curso y materia salen de las asignaciones activas
        if (!string.IsNullOrWhiteSpace(nivel) ||
            !string.IsNullOrWhiteSpace(curso) ||
            !string.IsNullOrWhiteSpace(materia))
        {
            query = query.Where(m => m.AsignacionesDocentes.Any(a =>
                a.Activo &&
                (string.IsNullOrEmpty(nivel) || a.Curso.Grado.Nivel.Nombre == nivel) &&
                (string.IsNullOrEmpty(curso) || a.Curso.Grado.Nombre + " - " + a.Curso.Nombre == curso) &&
                (string.IsNullOrEmpty(materia) || a.Materia.Nombre == materia)));
        }

        var resultado = await query
            .OrderBy(m => m.Nombres)
            .ThenBy(m => m.Apellidos)
            .ThenBy(m => m.IdMaestro)
            .Select(m => new
            {
                m.IdMaestro,
                m.CentroId,
                Centro = m.Centro != null ? m.Centro.Nombre : null,
                m.CodigoEmpleado,
                m.Nombres,
                m.Apellidos,
                m.Cedula,
                m.Telefono,
                m.Correo,
                m.Direccion,
                m.Especialidad,
                m.FechaIngreso,
                m.Activo,
                Usuario = m.Usuario != null ? m.Usuario.NombreUsuario : null,
                Niveles = m.AsignacionesDocentes
                    .Where(a => a.Activo)
                    .Select(a => a.Curso.Grado.Nivel.Nombre)
                    .Distinct()
                    .ToList()
            })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("filtros")]
    public async Task<IActionResult> GetFiltros()
    {
        var maestros = _context.Maestros.AsQueryable();

        if (!EsAdministrador)
        {
            maestros = maestros.Where(m => m.CentroId == IdCentro);
        }

        var datos = await maestros
            .Select(m => new
            {
                Centro = m.Centro != null ? m.Centro.Nombre : null,
                m.Especialidad
            })
            .Distinct()
            .ToListAsync();

        // Combinaciones existentes para que cada filtro dependa del anterior
        var asignaciones = await maestros
            .SelectMany(m => m.AsignacionesDocentes
                .Where(a => a.Activo)
                .Select(a => new
                {
                    Centro = m.Centro != null ? m.Centro.Nombre : null,
                    Nivel = a.Curso.Grado.Nivel.Nombre,
                    Curso = a.Curso.Grado.Nombre + " - " + a.Curso.Nombre,
                    Materia = a.Materia.Nombre
                }))
            .Distinct()
            .ToListAsync();

        return Ok(new { datos, asignaciones });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var maestro = await _context.Maestros
            .Include(m => m.Usuario)
            .Include(m => m.Centro)
            .FirstOrDefaultAsync(m => m.IdMaestro == id);

        if (maestro == null)
            return NotFound("Maestro no encontrado.");

        if (!EsAdministrador && maestro.CentroId != IdCentro)
            return Forbid();

      

        return Ok(maestro);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearMaestroDto dto)
    {
        var rolMaestro = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Maestro");

        if (rolMaestro == null)
            return BadRequest("No existe el rol Maestro.");

        if (!string.IsNullOrWhiteSpace(dto.CodigoEmpleado))
        {
            var codigoExiste = await _context.Maestros
                .AnyAsync(m => m.CodigoEmpleado == dto.CodigoEmpleado);

            if (codigoExiste)
                return BadRequest("Ya existe un maestro con ese código de empleado.");
        }

        var nombreUsuario = await _usuarioGenerator.GenerarNombreUsuarioAsync(dto.Nombres, dto.Apellidos);
        var passwordTemporal = _usuarioGenerator.GenerarPasswordTemporal();


        int centroGuardar = EsAdministrador
        ? dto.CentroId!.Value
                 : IdCentro!.Value;

        var usuario = new Usuario
        {
            IdRol = rolMaestro.IdRol,
            NombreUsuario = nombreUsuario,
            CentroId = centroGuardar,
            PasswordHash = _usuarioGenerator.HashearPassword(passwordTemporal),
            DebeCambiarPassword = true,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var maestro = new Maestro
        {
            IdUsuario = usuario.IdUsuario,
            CodigoEmpleado = dto.CodigoEmpleado,
            CentroId = centroGuardar,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Cedula = dto.Cedula,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            Especialidad = dto.Especialidad,
            FechaIngreso = dto.FechaIngreso,
            Activo = true
        };

        _context.Maestros.Add(maestro);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Maestro creado correctamente.",
            maestro.IdMaestro,
            maestro.Nombres,
            maestro.Apellidos,
            Usuario = nombreUsuario,
            PasswordTemporal = passwordTemporal
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, CrearMaestroDto dto)
    {
        var maestro = await _context.Maestros
     .FirstOrDefaultAsync(m => m.IdMaestro == id);

        if (maestro == null)
            return NotFound("Maestro no encontrado.");

        if (!string.IsNullOrWhiteSpace(dto.CodigoEmpleado))
        {
            var codigoExiste = await _context.Maestros
                .AnyAsync(m => m.CodigoEmpleado == dto.CodigoEmpleado && m.IdMaestro != id);

            if (codigoExiste)
                return BadRequest("Ya existe otro maestro con ese código de empleado.");
        }

        maestro.CodigoEmpleado = dto.CodigoEmpleado;
        maestro.Nombres = dto.Nombres;
        maestro.Apellidos = dto.Apellidos;
        maestro.Cedula = dto.Cedula;
        maestro.Telefono = dto.Telefono;
        maestro.Correo = dto.Correo;
        maestro.Direccion = dto.Direccion;
        maestro.Especialidad = dto.Especialidad;
        maestro.FechaIngreso = dto.FechaIngreso;


        maestro.CentroId = EsAdministrador
    ? dto.CentroId!.Value
    : IdCentro!.Value;
        await _context.SaveChangesAsync();

        return Ok(maestro);
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var maestro = await _context.Maestros
            .Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.IdMaestro == id);

        if (maestro == null)
            return NotFound("Maestro no encontrado.");

        maestro.Activo = !maestro.Activo;

        if (!EsAdministrador && maestro.CentroId != IdCentro)
            return Forbid();

        if (maestro.Usuario != null)
        {
            maestro.Usuario.Activo = maestro.Activo;
        }


        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado del maestro actualizado.",
            maestro.IdMaestro,
            maestro.Activo
        });
    }
}
