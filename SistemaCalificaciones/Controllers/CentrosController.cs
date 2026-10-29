using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;

namespace SistemaCalificaciones.Controllers;

// Solo lectura: lista de centros educativos para los formularios del panel.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico")]
public class CentrosController : BaseController
{
    private readonly AppDbContext _context;

    public CentrosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = _context.Centros.AsQueryable();

        // Un coordinador solo ve su propio centro
        if (!EsAdministrador)
        {
            query = query.Where(c => c.Id == IdCentro);
        }

        var centros = await query
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.Codigo,
                c.Estado
            })
            .ToListAsync();

        return Ok(centros);
    }
}
