namespace SistemaCalificaciones.DTOs
{
    public class CrearCoordinadorDto
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public int IdRol { get; set; }

        public int CentroId { get; set; }
    }
}
