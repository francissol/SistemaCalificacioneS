namespace SistemaCalificaciones.DTOs
{
    public class EditarCoordinadorDto
    {
        public int IdUsuario { get; set; }

        public int IdRol { get; set; }

        public int CentroId { get; set; }

        public bool Activo { get; set; }
    }
}
