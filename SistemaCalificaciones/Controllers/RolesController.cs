using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;

namespace SistemaCalificaciones.Controllers;

// Solo lectura: lista de roles para el formulario de coordinadores.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Nombre)
            .Select(r => new
            {
                r.IdRol,
                r.Nombre
            })
            .ToListAsync();

        return Ok(roles);
    }
}
