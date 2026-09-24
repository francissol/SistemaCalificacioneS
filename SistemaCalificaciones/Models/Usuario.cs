using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalificaciones.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }
    public Rol Rol { get; set; } = null!;


    public int? CentroId { get; set; }

    [ForeignKey("CentroId")]
    public Centro? Centro { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public bool DebeCambiarPassword { get; set; } = true;
    public bool Activo { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;


    public Maestro? Maestro { get; set; }
 
}