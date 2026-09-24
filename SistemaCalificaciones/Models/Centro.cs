using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SistemaCalificaciones.Models
{

    [Table("Centros")]
    public class Centro
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Codigo { get; set; }

        public bool Estado { get; set; } = true;
    }
}