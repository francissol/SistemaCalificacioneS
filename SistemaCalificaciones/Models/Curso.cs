using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalificaciones.Models
{


    public class Curso
    {
        public int IdCurso { get; set; }

        public int IdGrado { get; set; }
        public Grado Grado { get; set; } = null!;
        public int CentroId { get; set; }

        [ForeignKey("CentroId")]
        public Centro? Centro { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Seccion { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
        public ICollection<AsignacionDocente> AsignacionesDocentes { get; set; } = new List<AsignacionDocente>();
    }
}