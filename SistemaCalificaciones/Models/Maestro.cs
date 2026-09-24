using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalificaciones.Models
{

    public class Maestro
    {
        public int IdMaestro { get; set; }

        public int? IdUsuario { get; set; }
        public Usuario? Usuario { get; set; }

        // Agregar estas líneas dentro de la clase Maestro:
        public int? CentroId { get; set; }

        [ForeignKey("CentroId")]
        public Centro? Centro { get; set; }
        public string? CodigoEmpleado { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;

        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Especialidad { get; set; }

        public DateTime? FechaIngreso { get; set; }
        public bool Activo { get; set; } = true;

        public ICollection<AsignacionDocente> AsignacionesDocentes { get; set; } = new List<AsignacionDocente>();
    }
}