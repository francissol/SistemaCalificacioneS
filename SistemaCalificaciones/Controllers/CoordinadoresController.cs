using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.DTOs;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;

namespace SistemaCalificaciones.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrador")]
public class CoordinadoresController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UsuarioGeneratorService _generator;

    public CoordinadoresController(
        AppDbContext context,
        UsuarioGeneratorService generator)
    {
        _context = context;
        _generator = generator;
    }

    //--------------------------------------------------------
    // LISTAR
    //--------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var usuarios = await _context.Usuarios
            .Include(x => x.Rol)
            .Include(x => x.Centro)
            .Where(x => x.Rol.Nombre.StartsWith("Coordinador"))
            .Select(x => new
            {
                x.IdUsuario,
                x.NombreUsuario,
                Rol = x.Rol.Nombre,
                CentroId = x.CentroId,
                Centro = x.Centro != null ? x.Centro.Nombre : "Sin Centro",
                x.Activo
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    //--------------------------------------------------------
    // CREAR
    //--------------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Crear(CrearCoordinadorDto dto)
    {
        var rol = await _context.Roles
            .FirstOrDefaultAsync(r =>
                r.IdRol == dto.IdRol &&
                r.Nombre.StartsWith("Coordinador"));

        if (rol == null)
            return BadRequest(
                "El rol seleccionado no es un rol de coordinación.");

        var centroExiste = await _context.Centros
            .AnyAsync(c => c.Id == dto.CentroId);

        if (!centroExiste)
            return BadRequest(
                "El centro educativo seleccionado no existe.");

        var username = await _generator.GenerarNombreUsuarioAsync(
            dto.Nombres,
            dto.Apellidos);

        var passwordTemporal =
            _generator.GenerarPasswordTemporal();

        var usuario = new Usuario
        {
            NombreUsuario = username,
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(passwordTemporal),
            IdRol = dto.IdRol,
            CentroId = dto.CentroId,
            Activo = true,
            DebeCambiarPassword = true,
            FechaCreacion = DateTime.Now
        };

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Coordinador creado correctamente",
            usuario = username,
            password = passwordTemporal,
            rol = rol.Nombre,
            centroId = dto.CentroId
        });
    }
    //--------------------------------------------------------
    // EDITAR
    //--------------------------------------------------------

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(
        int id,
        EditarCoordinadorDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound("Coordinador no encontrado.");

        var centroExiste = await _context.Centros.AnyAsync(c => c.Id == dto.CentroId);
        if (!centroExiste)
            return BadRequest("El centro educativo seleccionado no existe.");

        usuario.IdRol = dto.IdRol;
        usuario.CentroId = dto.CentroId;
        usuario.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Coordinador actualizado correctamente." });
    }

    //--------------------------------------------------------
    // ACTIVAR / DESACTIVAR
    //--------------------------------------------------------

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound("Coordinador no encontrado.");

        usuario.Activo = !usuario.Activo;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado actualizado correctamente.",
            usuario.IdUsuario,
            usuario.Activo
        });
    }

    //--------------------------------------------------------
    // RESET PASSWORD
    //--------------------------------------------------------

    [HttpPut("{id}/reset")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound("Coordinador no encontrado.");

        var nuevaPassword = _generator.GenerarPasswordTemporal();

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
        usuario.DebeCambiarPassword = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Contraseña restablecida correctamente.",
            passwordTemporal = nuevaPassword
        });
    }
}