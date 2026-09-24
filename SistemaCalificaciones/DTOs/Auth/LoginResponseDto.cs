namespace SistemaCalificaciones.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    // --- Campos de Centro agregados ---
    public int? IdCentro { get; set; }
    public string? Centro { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool DebeCambiarPassword { get; set; }
}