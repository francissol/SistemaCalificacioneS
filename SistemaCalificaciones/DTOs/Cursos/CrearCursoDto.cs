namespace SistemaCalificaciones.DTOs.Cursos;

public class CrearCursoDto
{
    public int IdGrado { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? IdCentro { get; set; }


    public string? Seccion { get; set; }
}