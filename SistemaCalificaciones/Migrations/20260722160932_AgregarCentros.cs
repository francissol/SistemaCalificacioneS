using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalificaciones.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCentros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_AsignacionesDocentes_AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Competencias_CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Estudiantes_EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_PeriodosPublicacion_PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Competencias_CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Grados_GradoIdGrado",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Materias_MateriaIdMateria",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_GradoIdGrado",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_MateriaIdMateria",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropColumn(
                name: "CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropColumn(
                name: "GradoIdGrado",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropColumn(
                name: "MateriaIdMateria",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropColumn(
                name: "AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropColumn(
                name: "CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropColumn(
                name: "EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropColumn(
                name: "PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.AddColumn<int>(
                name: "IdCentro",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsTecnica",
                table: "Materias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IdCentro",
                table: "Maestros",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CentroIdCentro",
                table: "Estudiantes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdCentro",
                table: "Estudiantes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdCentro",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "IdPeriodoPublicacion",
                table: "ActividadesCompetencias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdCompetencia",
                table: "ActividadesCompetencias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdResultadoAprendizaje",
                table: "ActividadesCompetencias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Centros",
                columns: table => new
                {
                    IdCentro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCentro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centros", x => x.IdCentro);
                });

            migrationBuilder.CreateTable(
                name: "CursoMaterias",
                columns: table => new
                {
                    IdCursoMateria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCurso = table.Column<int>(type: "int", nullable: false),
                    CursoIdCurso = table.Column<int>(type: "int", nullable: false),
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    MateriaIdMateria = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CursoMaterias", x => x.IdCursoMateria);
                    table.ForeignKey(
                        name: "FK_CursoMaterias_Cursos_CursoIdCurso",
                        column: x => x.CursoIdCurso,
                        principalTable: "Cursos",
                        principalColumn: "IdCurso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CursoMaterias_Materias_MateriaIdMateria",
                        column: x => x.MateriaIdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosAprendizaje",
                columns: table => new
                {
                    IdResultadoAprendizaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsignacionDocente = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValorMaximo = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosAprendizaje", x => x.IdResultadoAprendizaje);
                    table.ForeignKey(
                        name: "FK_ResultadosAprendizaje_AsignacionesDocentes_IdAsignacionDocente",
                        column: x => x.IdAsignacionDocente,
                        principalTable: "AsignacionesDocentes",
                        principalColumn: "IdAsignacionDocente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdCentro",
                table: "Usuarios",
                column: "IdCentro");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCompetencias_IdActividadCompetencia",
                table: "NotasCompetencias",
                column: "IdActividadCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCompetencias_IdEstudiante",
                table: "NotasCompetencias",
                column: "IdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_Maestros_IdCentro",
                table: "Maestros",
                column: "IdCentro");

            migrationBuilder.CreateIndex(
                name: "IX_Estudiantes_CentroIdCentro",
                table: "Estudiantes",
                column: "CentroIdCentro");

            migrationBuilder.CreateIndex(
                name: "IX_Cursos_IdCentro",
                table: "Cursos",
                column: "IdCentro");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_IdCompetencia",
                table: "CompetenciasGradoMateria",
                column: "IdCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_IdGrado",
                table: "CompetenciasGradoMateria",
                column: "IdGrado");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_IdMateria",
                table: "CompetenciasGradoMateria",
                column: "IdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdAsignacionDocente");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdCompetencia",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdEstudiante",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdPeriodoPublicacion");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesCompetencias_IdAsignacionDocente",
                table: "ActividadesCompetencias",
                column: "IdAsignacionDocente");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesCompetencias_IdCompetencia",
                table: "ActividadesCompetencias",
                column: "IdCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesCompetencias_IdPeriodoPublicacion",
                table: "ActividadesCompetencias",
                column: "IdPeriodoPublicacion");

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesCompetencias_IdResultadoAprendizaje",
                table: "ActividadesCompetencias",
                column: "IdResultadoAprendizaje");

            migrationBuilder.CreateIndex(
                name: "IX_CursoMaterias_CursoIdCurso",
                table: "CursoMaterias",
                column: "CursoIdCurso");

            migrationBuilder.CreateIndex(
                name: "IX_CursoMaterias_MateriaIdMateria",
                table: "CursoMaterias",
                column: "MateriaIdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosAprendizaje_IdAsignacionDocente",
                table: "ResultadosAprendizaje",
                column: "IdAsignacionDocente");

            migrationBuilder.AddForeignKey(
                name: "FK_ActividadesCompetencias_AsignacionesDocentes_IdAsignacionDocente",
                table: "ActividadesCompetencias",
                column: "IdAsignacionDocente",
                principalTable: "AsignacionesDocentes",
                principalColumn: "IdAsignacionDocente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActividadesCompetencias_Competencias_IdCompetencia",
                table: "ActividadesCompetencias",
                column: "IdCompetencia",
                principalTable: "Competencias",
                principalColumn: "IdCompetencia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActividadesCompetencias_PeriodosPublicacion_IdPeriodoPublicacion",
                table: "ActividadesCompetencias",
                column: "IdPeriodoPublicacion",
                principalTable: "PeriodosPublicacion",
                principalColumn: "IdPeriodoPublicacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActividadesCompetencias_ResultadosAprendizaje_IdResultadoAprendizaje",
                table: "ActividadesCompetencias",
                column: "IdResultadoAprendizaje",
                principalTable: "ResultadosAprendizaje",
                principalColumn: "IdResultadoAprendizaje",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_AsignacionesDocentes_IdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdAsignacionDocente",
                principalTable: "AsignacionesDocentes",
                principalColumn: "IdAsignacionDocente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Competencias_IdCompetencia",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdCompetencia",
                principalTable: "Competencias",
                principalColumn: "IdCompetencia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Estudiantes_IdEstudiante",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdEstudiante",
                principalTable: "Estudiantes",
                principalColumn: "IdEstudiante",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_PeriodosPublicacion_IdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo",
                column: "IdPeriodoPublicacion",
                principalTable: "PeriodosPublicacion",
                principalColumn: "IdPeriodoPublicacion",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Competencias_IdCompetencia",
                table: "CompetenciasGradoMateria",
                column: "IdCompetencia",
                principalTable: "Competencias",
                principalColumn: "IdCompetencia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Grados_IdGrado",
                table: "CompetenciasGradoMateria",
                column: "IdGrado",
                principalTable: "Grados",
                principalColumn: "IdGrado",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Materias_IdMateria",
                table: "CompetenciasGradoMateria",
                column: "IdMateria",
                principalTable: "Materias",
                principalColumn: "IdMateria",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cursos_Centros_IdCentro",
                table: "Cursos",
                column: "IdCentro",
                principalTable: "Centro",
                principalColumn: "IdCentro",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Estudiantes_Centros_CentroIdCentro",
                table: "Estudiantes",
                column: "CentroIdCentro",
                principalTable: "Centros",
                principalColumn: "IdCentro",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Maestros_Centros_IdCentro",
                table: "Maestros",
                column: "IdCentro",
                principalTable: "Centros",
                principalColumn: "IdCentro",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasCompetencias_ActividadesCompetencias_IdActividadCompetencia",
                table: "NotasCompetencias",
                column: "IdActividadCompetencia",
                principalTable: "ActividadesCompetencias",
                principalColumn: "IdActividadCompetencia",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasCompetencias_Estudiantes_IdEstudiante",
                table: "NotasCompetencias",
                column: "IdEstudiante",
                principalTable: "Estudiantes",
                principalColumn: "IdEstudiante",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Centros_IdCentro",
                table: "Usuarios",
                column: "IdCentro",
                principalTable: "Centros",
                principalColumn: "IdCentro",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActividadesCompetencias_AsignacionesDocentes_IdAsignacionDocente",
                table: "ActividadesCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_ActividadesCompetencias_Competencias_IdCompetencia",
                table: "ActividadesCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_ActividadesCompetencias_PeriodosPublicacion_IdPeriodoPublicacion",
                table: "ActividadesCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_ActividadesCompetencias_ResultadosAprendizaje_IdResultadoAprendizaje",
                table: "ActividadesCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_AsignacionesDocentes_IdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Competencias_IdCompetencia",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Estudiantes_IdEstudiante",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_PeriodosPublicacion_IdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Competencias_IdCompetencia",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Grados_IdGrado",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetenciasGradoMateria_Materias_IdMateria",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_Cursos_Centros_IdCentro",
                table: "Cursos");

            migrationBuilder.DropForeignKey(
                name: "FK_Estudiantes_Centros_CentroIdCentro",
                table: "Estudiantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Maestros_Centros_IdCentro",
                table: "Maestros");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasCompetencias_ActividadesCompetencias_IdActividadCompetencia",
                table: "NotasCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasCompetencias_Estudiantes_IdEstudiante",
                table: "NotasCompetencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Centros_IdCentro",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Centros");

            migrationBuilder.DropTable(
                name: "CursoMaterias");

            migrationBuilder.DropTable(
                name: "ResultadosAprendizaje");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_IdCentro",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_NotasCompetencias_IdActividadCompetencia",
                table: "NotasCompetencias");

            migrationBuilder.DropIndex(
                name: "IX_NotasCompetencias_IdEstudiante",
                table: "NotasCompetencias");

            migrationBuilder.DropIndex(
                name: "IX_Maestros_IdCentro",
                table: "Maestros");

            migrationBuilder.DropIndex(
                name: "IX_Estudiantes_CentroIdCentro",
                table: "Estudiantes");

            migrationBuilder.DropIndex(
                name: "IX_Cursos_IdCentro",
                table: "Cursos");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_IdCompetencia",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_IdGrado",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CompetenciasGradoMateria_IdMateria",
                table: "CompetenciasGradoMateria");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdCompetencia",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdEstudiante",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_IdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo");

            migrationBuilder.DropIndex(
                name: "IX_ActividadesCompetencias_IdAsignacionDocente",
                table: "ActividadesCompetencias");

            migrationBuilder.DropIndex(
                name: "IX_ActividadesCompetencias_IdCompetencia",
                table: "ActividadesCompetencias");

            migrationBuilder.DropIndex(
                name: "IX_ActividadesCompetencias_IdPeriodoPublicacion",
                table: "ActividadesCompetencias");

            migrationBuilder.DropIndex(
                name: "IX_ActividadesCompetencias_IdResultadoAprendizaje",
                table: "ActividadesCompetencias");

            migrationBuilder.DropColumn(
                name: "IdCentro",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EsTecnica",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "IdCentro",
                table: "Maestros");

            migrationBuilder.DropColumn(
                name: "CentroIdCentro",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "IdCentro",
                table: "Estudiantes");

            migrationBuilder.DropColumn(
                name: "IdCentro",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "IdResultadoAprendizaje",
                table: "ActividadesCompetencias");

            migrationBuilder.AddColumn<int>(
                name: "CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GradoIdGrado",
                table: "CompetenciasGradoMateria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MateriaIdMateria",
                table: "CompetenciasGradoMateria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "IdPeriodoPublicacion",
                table: "ActividadesCompetencias",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdCompetencia",
                table: "ActividadesCompetencias",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria",
                column: "CompetenciaIdCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_GradoIdGrado",
                table: "CompetenciasGradoMateria",
                column: "GradoIdGrado");

            migrationBuilder.CreateIndex(
                name: "IX_CompetenciasGradoMateria_MateriaIdMateria",
                table: "CompetenciasGradoMateria",
                column: "MateriaIdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo",
                column: "AsignacionDocenteIdAsignacionDocente");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo",
                column: "CompetenciaIdCompetencia");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo",
                column: "EstudianteIdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesCompetenciasPeriodo_PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo",
                column: "PeriodoPublicacionIdPeriodoPublicacion");

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_AsignacionesDocentes_AsignacionDocenteIdAsignacionDocente",
                table: "CalificacionesCompetenciasPeriodo",
                column: "AsignacionDocenteIdAsignacionDocente",
                principalTable: "AsignacionesDocentes",
                principalColumn: "IdAsignacionDocente",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Competencias_CompetenciaIdCompetencia",
                table: "CalificacionesCompetenciasPeriodo",
                column: "CompetenciaIdCompetencia",
                principalTable: "Competencias",
                principalColumn: "IdCompetencia",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_Estudiantes_EstudianteIdEstudiante",
                table: "CalificacionesCompetenciasPeriodo",
                column: "EstudianteIdEstudiante",
                principalTable: "Estudiantes",
                principalColumn: "IdEstudiante",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesCompetenciasPeriodo_PeriodosPublicacion_PeriodoPublicacionIdPeriodoPublicacion",
                table: "CalificacionesCompetenciasPeriodo",
                column: "PeriodoPublicacionIdPeriodoPublicacion",
                principalTable: "PeriodosPublicacion",
                principalColumn: "IdPeriodoPublicacion",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Competencias_CompetenciaIdCompetencia",
                table: "CompetenciasGradoMateria",
                column: "CompetenciaIdCompetencia",
                principalTable: "Competencias",
                principalColumn: "IdCompetencia",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Grados_GradoIdGrado",
                table: "CompetenciasGradoMateria",
                column: "GradoIdGrado",
                principalTable: "Grados",
                principalColumn: "IdGrado",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetenciasGradoMateria_Materias_MateriaIdMateria",
                table: "CompetenciasGradoMateria",
                column: "MateriaIdMateria",
                principalTable: "Materias",
                principalColumn: "IdMateria",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
