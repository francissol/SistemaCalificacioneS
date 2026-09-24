using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalificaciones.Data;
using SistemaCalificaciones.Models;
using SistemaCalificaciones.Services;

namespace SistemaCalificaciones.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,CoordinadorPrimaria,CoordinadorSecundaria,CoordinadorPolitecnico")]
public class ImportacionExcelController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UsuarioGeneratorService _usuarioGenerator;

    public ImportacionExcelController(
        AppDbContext context,
        UsuarioGeneratorService usuarioGenerator)
    {
        _context = context;
        _usuarioGenerator = usuarioGenerator;
    }

    // ============================================================
    // IMPORTAR ESTUDIANTES
    // ============================================================

    [HttpPost("estudiantes")]
    public async Task<IActionResult> ImportarEstudiantes(
        IFormFile archivo,
        int idAnioEscolar)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest("Debe subir un archivo Excel.");

        var anio = await _context.AniosEscolares.FindAsync(idAnioEscolar);

        if (anio == null || anio.Cerrado)
            return BadRequest("El año escolar no existe o está cerrado.");

        int total = 0;
        int exitosos = 0;
        int fallidos = 0;

        var errores = new List<object>();

        using var stream = new MemoryStream();
        await archivo.CopyToAsync(stream);

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            total++;

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // ====================================================
                // DATOS DEL ESTUDIANTE
                // ====================================================

                string matricula = row.Cell(1).GetString().Trim();
                string nombres = row.Cell(2).GetString().Trim();
                string apellidos = row.Cell(3).GetString().Trim();

                DateTime? fechaNacimiento = null;

                if (DateTime.TryParse(
                    row.Cell(4).GetString(),
                    out var fechaNac))
                {
                    fechaNacimiento = fechaNac;
                }

                string? sexo = row.Cell(5).GetString().Trim();
                string? telefono = row.Cell(6).GetString().Trim();
                string? correo = row.Cell(7).GetString().Trim();
                string? direccion = row.Cell(8).GetString().Trim();

                // ====================================================
                // CURSO Y NIVEL
                // ====================================================

                string cursoNombre = row.Cell(9).GetString().Trim();
                string nivelNombre = row.Cell(10).GetString().Trim();

                // ====================================================
                // DATOS DEL PADRE
                // ====================================================

                string nombrePadre = row.Cell(11).GetString().Trim();
                string apellidoPadre = row.Cell(12).GetString().Trim();
                string telefonoPadre = row.Cell(13).GetString().Trim();
                string correoPadre = row.Cell(14).GetString().Trim();
                string parentesco = row.Cell(15).GetString().Trim();

                // ====================================================
                // CENTRO
                // ====================================================

                if (!int.TryParse(
                    row.Cell(16).GetString().Trim(),
                    out int centroId))
                {
                    throw new Exception("El CentroId no es válido.");
                }

                var centro = await _context.Centros
                    .FirstOrDefaultAsync(c =>
                        c.Id == centroId &&
                        c.Estado);

                if (centro == null)
                {
                    throw new Exception(
                        $"No existe un centro activo con ID {centroId}.");
                }

                // ====================================================
                // VALIDACIONES
                // ====================================================

                if (string.IsNullOrWhiteSpace(matricula))
                    throw new Exception("La matrícula está vacía.");

                if (string.IsNullOrWhiteSpace(nombres) ||
                    string.IsNullOrWhiteSpace(apellidos))
                {
                    throw new Exception(
                        "El nombre o apellido del estudiante está vacío.");
                }

                if (string.IsNullOrWhiteSpace(cursoNombre))
                    throw new Exception("El curso está vacío.");

                if (string.IsNullOrWhiteSpace(nivelNombre))
                    throw new Exception("El nivel está vacío.");

                // ====================================================
                // VALIDAR QUE NO EXISTA EL ESTUDIANTE
                // ====================================================

                var existeEstudiante = await _context.Estudiantes
                    .AnyAsync(e => e.Matricula == matricula);

                if (existeEstudiante)
                {
                    throw new Exception(
                        $"Ya existe un estudiante con matrícula {matricula}.");
                }

                // ====================================================
                // BUSCAR CURSO
                // ====================================================

                var cursoNombreNormalizado =
                    cursoNombre.Replace(" ", "").ToLower();

                var nivelNombreNormalizado =
                    nivelNombre.Trim().ToLower();

                var curso = await _context.Cursos
                    .Include(c => c.Grado)
                        .ThenInclude(g => g.Nivel)
                    .FirstOrDefaultAsync(c =>
                        c.Nombre.Replace(" ", "").ToLower()
                            == cursoNombreNormalizado &&

                        c.Grado.Nivel.Nombre.ToLower()
                            == nivelNombreNormalizado &&

                        c.CentroId == centroId &&

                        c.Activo
                    );

                if (curso == null)
                {
                    throw new Exception(
                        $"No existe el curso {cursoNombre} " +
                        $"del nivel {nivelNombre} " +
                        $"en el centro {centro.Nombre}.");
                }

                // ====================================================
                // CREAR ESTUDIANTE
                // ====================================================

                var estudiante = new Estudiante
                {
                    Matricula = matricula,
                    Nombres = nombres,
                    Apellidos = apellidos,

                    CentroId = centroId,

                    FechaNacimiento = fechaNacimiento,
                    Sexo = sexo,
                    Telefono = telefono,
                    Correo = correo,
                    Direccion = direccion,

                    FechaIngreso = DateTime.Now,
                    Activo = true
                };

                _context.Estudiantes.Add(estudiante);

                await _context.SaveChangesAsync();

                // ====================================================
                // CREAR INSCRIPCIÓN
                // ====================================================

                var inscripcion = new Inscripcion
                {
                    IdEstudiante = estudiante.IdEstudiante,
                    IdCurso = curso.IdCurso,
                    IdAnioEscolar = idAnioEscolar,
                    FechaInscripcion = DateTime.Now,
                    Estado = "Activo"
                };

                _context.Inscripciones.Add(inscripcion);

                // ====================================================
                // PADRE
                // ====================================================

                if (!string.IsNullOrWhiteSpace(nombrePadre) &&
                    !string.IsNullOrWhiteSpace(apellidoPadre))
                {
                    var padre = await _context.Padres
                        .FirstOrDefaultAsync(p =>
                            p.Nombres == nombrePadre &&
                            p.Apellidos == apellidoPadre &&
                            p.Telefono == telefonoPadre);

                    // -----------------------------------------------
                    // SI EL PADRE NO EXISTE, CREARLO
                    // -----------------------------------------------

                    if (padre == null)
                    {
                        padre = new Padre
                        {
                            Nombres = nombrePadre,
                            Apellidos = apellidoPadre,
                            Telefono = telefonoPadre,
                            Correo = correoPadre,
                            Activo = true
                        };

                        _context.Padres.Add(padre);

                        await _context.SaveChangesAsync();
                    }

                    // -----------------------------------------------
                    // RELACIONAR PADRE CON ESTUDIANTE
                    // -----------------------------------------------

                    var relacionExiste =
                        await _context.PadreEstudiantes.AnyAsync(pe =>
                            pe.IdPadre == padre.IdPadre &&
                            pe.IdEstudiante == estudiante.IdEstudiante);

                    if (!relacionExiste)
                    {
                        _context.PadreEstudiantes.Add(
                            new PadreEstudiante
                            {
                                IdPadre = padre.IdPadre,
                                IdEstudiante = estudiante.IdEstudiante,
                                Parentesco =
                                    string.IsNullOrWhiteSpace(parentesco)
                                        ? "Padre"
                                        : parentesco,
                                ResponsableAcademico = true
                            });
                    }
                }

                // ====================================================
                // GUARDAR TODO
                // ====================================================

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                exitosos++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                fallidos++;

                errores.Add(new
                {
                    Fila = row.RowNumber(),
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            mensaje = "Importación finalizada.",
            total,
            exitosos,
            fallidos,
            errores
        });
    }

    // ============================================================
    // IMPORTAR MAESTROS
    // ============================================================

    [HttpPost("maestros")]
    public async Task<IActionResult> ImportarMaestros(
        IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest("Debe subir un archivo Excel.");

        var rolMaestro = await _context.Roles
            .FirstOrDefaultAsync(r => r.Nombre == "Maestro");

        if (rolMaestro == null)
            return BadRequest("No existe el rol Maestro.");

        int total = 0;
        int exitosos = 0;
        int fallidos = 0;

        var errores = new List<object>();

        using var stream = new MemoryStream();
        await archivo.CopyToAsync(stream);

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            total++;

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                string codigoEmpleado =
                    row.Cell(1).GetString().Trim();

                string nombres =
                    row.Cell(2).GetString().Trim();

                string apellidos =
                    row.Cell(3).GetString().Trim();

                string cedula =
                    row.Cell(4).GetString().Trim();

                string telefono =
                    row.Cell(5).GetString().Trim();

                string correo =
                    row.Cell(6).GetString().Trim();

                string direccion =
                    row.Cell(7).GetString().Trim();

                string especialidad =
                    row.Cell(8).GetString().Trim();

                DateTime? fechaIngreso = null;

                if (DateTime.TryParse(
                    row.Cell(9).GetString(),
                    out var fechaIng))
                {
                    fechaIngreso = fechaIng;
                }

                if (string.IsNullOrWhiteSpace(nombres) ||
                    string.IsNullOrWhiteSpace(apellidos))
                {
                    throw new Exception(
                        "El nombre o apellido del maestro está vacío.");
                }

                if (!string.IsNullOrWhiteSpace(codigoEmpleado))
                {
                    var existeCodigo =
                        await _context.Maestros.AnyAsync(
                            m => m.CodigoEmpleado == codigoEmpleado);

                    if (existeCodigo)
                    {
                        throw new Exception(
                            $"Ya existe un maestro con el código {codigoEmpleado}.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(cedula))
                {
                    var existeCedula =
                        await _context.Maestros.AnyAsync(
                            m => m.Cedula == cedula);

                    if (existeCedula)
                    {
                        throw new Exception(
                            $"Ya existe un maestro con la cédula {cedula}.");
                    }
                }

                // ====================================================
                // CREAR USUARIO DEL MAESTRO
                // ====================================================

                var nombreUsuario =
                    await _usuarioGenerator
                        .GenerarNombreUsuarioAsync(
                            nombres,
                            apellidos);

                var passwordTemporal =
                    _usuarioGenerator.GenerarPasswordTemporal();

                var usuario = new Usuario
                {
                    IdRol = rolMaestro.IdRol,
                    NombreUsuario = nombreUsuario,
                    PasswordHash =
                        _usuarioGenerator
                            .HashearPassword(passwordTemporal),
                    DebeCambiarPassword = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                _context.Usuarios.Add(usuario);

                await _context.SaveChangesAsync();

                // ====================================================
                // CREAR MAESTRO
                // ====================================================

                var maestro = new Maestro
                {
                    IdUsuario = usuario.IdUsuario,

                    CodigoEmpleado = codigoEmpleado,

                    Nombres = nombres,
                    Apellidos = apellidos,

                    Cedula = cedula,
                    Telefono = telefono,
                    Correo = correo,
                    Direccion = direccion,

                    Especialidad = especialidad,
                    FechaIngreso = fechaIngreso,

                    Activo = true
                };

                _context.Maestros.Add(maestro);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                exitosos++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                fallidos++;

                errores.Add(new
                {
                    Fila = row.RowNumber(),
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            mensaje = "Importación de maestros finalizada.",
            total,
            exitosos,
            fallidos,
            errores
        });
    }
}