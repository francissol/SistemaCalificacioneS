using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalificaciones.Models
{

    public class Estudiante
    {
        public int IdEstudiante { get; set; }

        public string Matricula { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;

        public DateTime? FechaNacimiento { get; set; }
        public string? Sexo { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        
        public int CentroId { get; set; }

        [ForeignKey("CentroId")]
        public Centro? Centro { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public bool Activo { get; set; } = true;

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
        public ICollection<PadreEstudiante> PadreEstudiantes { get; set; } = new List<PadreEstudiante>();
        public ICollection<CalificacionPeriodo> CalificacionesPeriodo { get; set; } = new List<CalificacionPeriodo>();
    }
}