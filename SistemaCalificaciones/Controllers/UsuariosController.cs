using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;
using SistemaCalificaciones.DTOs.Auth;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")] 
public class UsuariosController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UsuarioGeneratorService _usuarioGenerator;

    public UsuariosController(AppDbContext context, UsuarioGeneratorService usuarioGenerator)
    {
        _context = context;
        _usuarioGenerator = usuarioGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? centroId)
    {
        var query = _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Centro)
            .AsQueryable();

        // Si se pasa un centro por parámetro, se filtra
        if (centroId.HasValue && centroId.Value > 0)
        {
            query = query.Where(u => u.CentroId == centroId.Value);
        }

        var usuarios = await query
            .OrderBy(u => u.NombreUsuario)
            .Select(u => new
            {
                u.IdUsuario,
                u.NombreUsuario,
                u.CentroId,
                Centro = u.Centro != null ? u.Centro.Nombre : "Sin Centro (Global)",
                Rol = u.Rol.Nombre,
                u.Activo,
                u.DebeCambiarPassword,
                u.UltimoAcceso
            })
            .ToListAsync();

        return Ok(usuarios);
    }



    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound("Usuario no encontrado.");

        usuario.Activo = !usuario.Activo;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado del usuario actualizado.",
            usuario.IdUsuario,
            usuario.Activo
        });
    }
}

// DTO para la creación de coordinadores
public class CrearCoordinadorDto
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public int CentroId { get; set; }
}