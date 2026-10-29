# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ASP.NET Core 9 Web API (`SistemaCalificaciones/`) for a school grading system: courses, teachers, students, parents, graded activities, period grades, competency-based grading, Excel import and report/PDF generation. The frontend is a separate React/Vite app (not in this repo) deployed on Vercel. Code, identifiers, API messages and comments are in Spanish — keep new code consistent with that.

There is no test project and no linter configured.

## Commands

Run from `SistemaCalificaciones/` (the solution file is `SistemaCalificaciones.slnx` at the repo root):

```bash
dotnet build
dotnet run                                    # http://localhost:5262 (https profile: https://localhost:7023)
dotnet ef migrations add <Nombre>             # EF Core migrations (Tools package is referenced)
dotnet ef database update
```

Swagger UI is enabled in all environments at `/swagger`; use the "Authorize" button with a JWT from `POST /api/Auth/login`.

The database is SQL Server, `(localdb)\MSSQLLocalDB` / `SistemaCalificaciones` by default (`appsettings.json`).

## Architecture

- **Thin layering**: controllers query `AppDbContext` directly with EF Core LINQ. There is no repository or service layer for business logic; the only services are `JwtService` (token creation) and `UsuarioGeneratorService` (generates usernames such as `JP1` from initials, a fixed temporary password, and BCrypt hashing). Request bodies use DTOs under `DTOs/<Area>/`; responses are usually anonymous projections.
- **`Data/AppDbContext.cs`** holds all Fluent configuration: explicit keys (models use `Id<Entity>` naming, except `Centro.Id`), unique indexes, and nearly every FK set to `DeleteBehavior.Restrict`. Deletes of parent entities therefore fail if children exist, so controllers often soft-deactivate (`Activo`/`Activa`/`Estado` flags) instead.
- **Auth & roles**: JWT bearer. Claims are `NameIdentifier` (IdUsuario), `Role`, `DebeCambiarPassword`, plus `IdCentro`/`NombreCentro` when the user belongs to a centre. Roles: `Administrador`, `Maestro`, `Estudiante`, `Padre` (seeded via `HasData`), and `CoordinadorPrimaria`, `CoordinadorSecundaria`, `CoordinadorPolitecnico` (used in `[Authorize(Roles=...)]` but not seeded). `Helpers/NivelHelper` maps a coordinator role to its level name.
- **Multi-centre scoping**: `Helpers/BaseController` (namespace `SistemaCalificaciones.Controllers`) exposes `IdUsuario`, `Rol`, `IdCentro`, `EsAdministrador`. Non-admin users must only see data from their own centre, filtered by hand in each action (`if (!EsAdministrador && x.CentroId != IdCentro)`). Not every controller inherits `BaseController` yet; some (e.g. `CursosController`) read the `IdCentro` claim inline. Follow this pattern in any new endpoint that touches centre-owned data.
- **Academic model**: `Nivel` → `Grado` → `Curso` (per `AnioEscolar` and `Centro`). `GradoMateria`/`CursoMateria` link subjects. `AsignacionDocente` (curso + materia + año + maestro) is the unit teachers grade against. Grading works in one of two ways, chosen by `Nivel.UsaCompetencias`:
  - **Activities / learning outcomes** (secondary/polytechnic): `ActividadEvaluativa` → `NotaActividad`, `ResultadoAprendizaje`/`CalificacionRA`, rolled up into `CalificacionPeriodo` by `CalificacionesPeriodoController`.
  - **Competencies** (primary): `Competencia` ↔ `CompetenciaGradoMateria`, `ActividadCompetencia` → `NotaCompetencia`, rolled up into `CalificacionCompetenciaPeriodo`.
  `PeriodoPublicacion` defines the grading periods and their publish windows.
- **Reports** (`ReportesController`, the largest file): per-level endpoints (`primaria/`, `secundaria/`, `politecnico/`, period and annual). The PDF endpoint stamps text at fixed coordinates onto `Templates/Secundaria/3ro.pdf` with iText7. The template path is resolved from `Directory.GetCurrentDirectory()` and is not copied to the build output, so the app must run with the project folder as its working directory.
- **Excel import** (`ImportacionExcelController`): ClosedXML bulk upload of students and teachers. It creates `Usuario` accounts through `UsuarioGeneratorService`.
- **CORS**: the `ReactPolicy` in `Program.cs` whitelists localhost:5173 and the Vercel frontend URLs. Origins must not have a trailing slash, or they won't match. Add new frontend deployment URLs there.

`WeatherForecast*` and `UserExtensions.cs` are leftover template/placeholder files.
