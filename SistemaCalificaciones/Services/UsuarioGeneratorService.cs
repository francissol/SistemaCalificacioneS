using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using System.Security.Cryptography;

namespace SistemaCalificaciones.Services;

public class UsuarioGeneratorService
{
    private readonly AppDbContext _context;

    public UsuarioGeneratorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerarNombreUsuarioAsync(string nombres, string apellidos)
    {
        var primeraLetraNombre = nombres.Trim()[0].ToString().ToUpper();
        var primeraLetraApellido = apellidos.Trim()[0].ToString().ToUpper();

        var baseUsuario = primeraLetraNombre + primeraLetraApellido;

        int contador = 1;
        string nombreUsuario;

        do
        {
            nombreUsuario = $"{baseUsuario}{contador}";
            contador++;
        }
        while (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario));

        return nombreUsuario;
    }

    // Sin caracteres ambiguos (0/O, 1/l/I) para que sea fácil de dictar o copiar.
    private const string CaracteresPassword = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public string GenerarPasswordTemporal()
    {
        return RandomNumberGenerator.GetString(CaracteresPassword, 10);
    }

    public string HashearPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}