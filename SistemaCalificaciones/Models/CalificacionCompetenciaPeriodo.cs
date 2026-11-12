using System.ComponentModel.DataAnnotations.Schema;
using SistemaCalificaciones.Helpers;

namespace SistemaCalificaciones.Models;

public class CalificacionCompetenciaPeriodo
{
    public int IdCalificacionCompetenciaPeriodo { get; set; }

    public int IdEstudiante { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public int IdAsignacionDocente { get; set; }
    public AsignacionDocente AsignacionDocente { get; set; } = null!;

    public int IdPeriodoPublicacion { get; set; }
    public PeriodoPublicacion PeriodoPublicacion { get; set; } = null!;

    public int IdCompetencia { get; set; }
    public Competencia Competencia { get; set; } = null!;

    // Nota original de la competencia en el período
    public decimal Promedio { get; set; }

    // Solo se permite cuando Promedio < 70; su valor máximo es 100 - Promedio (ver RecuperacionHelper)
    public decimal? NotaRecuperacion { get; set; }
    public DateTime? FechaRecuperacion { get; set; }

    // Calculada en memoria; no usar dentro de consultas LINQ que se traducen a SQL
    [NotMapped]
    public decimal NotaFinal => RecuperacionHelper.NotaFinal(Promedio, NotaRecuperacion);

    public bool Publicada { get; set; } = false;
    public DateTime? FechaPublicacion { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}