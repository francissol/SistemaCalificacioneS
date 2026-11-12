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
dotnet tool restore                           # installs the pinned local dotnet-ef (dotnet-tools.json)
dotnet ef migrations add <Nombre>             # EF Core migrations (Tools package is referenced)
dotnet ef database update
dotnet user-secrets set Jwt:Key "<at least 32 bytes>"   # required before first run
```

**Migrations are out of sync with the real schema.** The DB has been changed by hand: `AgregarModuloCompetenciasPrimaria` and `AgregarCentros` show as pending, but their changes are already in the DB, and the model snapshot doesn't match the model. `dotnet ef migrations add` therefore generates destructive operations, such as dropping `Estudiantes.IdUsuario` and `Padres.IdUsuario`. Don't run `database update`. Put schema changes in idempotent SQL scripts under `SistemaCalificaciones/Scripts/` instead, e.g. `AgregarRecuperacionCompetencias.sql`.

Startup throws if `Jwt:Key` is missing or shorter than 32 bytes. It is not in `appsettings.json`: set it with user-secrets locally or with the `Jwt__Key` env var in production. `appsettings.Production.json` is gitignored.

Swagger UI is only enabled in Development, at `/swagger`; use the "Authorize" button with a JWT from `POST /api/Auth/login`.

The database is SQL Server, `(localdb)\MSSQLLocalDB` / `SistemaCalificaciones` by default (`appsettings.json`).

The app is deployed behind a reverse proxy: `UseForwardedHeaders` (X-Forwarded-For/Proto) runs first in the pipeline, then CORS, which comes before HTTPS redirection so that preflight requests get their headers.

## Architecture

- **Thin layering**: controllers query `AppDbContext` directly with EF Core LINQ. There is no repository or service layer for business logic; the only services are `JwtService` (token creation), `UsuarioGeneratorService` (generates usernames such as `JP1` from initials, a fixed temporary password, and BCrypt hashing) and `CalculoRAService`. `CalculoRAService` holds the RA grade calculation: each RA is the sum of its active activities' grades, capped at the RA's `ValorMaximo`. It's shared by `CalificacionesRA/calcular`, `guardar-y-calcular` and the RA activity edit/delete endpoints, which recalculate only if grades were already calculated. Request bodies use DTOs under `DTOs/<Area>/`; responses are usually anonymous projections. Paged list endpoints (e.g. `GET .../paginado` in Estudiantes, Maestros, Padres) take `pagina`/`tamanoPagina` and use the `PaginarAsync` extension in `Helpers/Paginacion.cs`, which caps page size at 100 and expects an already-ordered query.
- **`Data/AppDbContext.cs`** holds all Fluent configuration: explicit keys (models use `Id<Entity>` naming, except `Centro.Id`), unique indexes, and nearly every FK set to `DeleteBehavior.Restrict`. Deletes of parent entities therefore fail if children exist, so controllers often soft-deactivate (`Activo`/`Activa`/`Estado` flags) instead.
- **Auth & roles**: JWT bearer. Claims are `NameIdentifier` (IdUsuario), `Role`, `DebeCambiarPassword`, plus `IdCentro`/`NombreCentro` when the user belongs to a centre. Roles: `Administrador`, `Maestro`, `Estudiante`, `Padre` (seeded via `HasData`), and `CoordinadorPrimaria`, `CoordinadorSecundaria`, `CoordinadorPolitecnico` (used in `[Authorize(Roles=...)]` but not seeded). `Helpers/NivelHelper` maps a coordinator role to its level name.
- **Multi-centre scoping**: `Helpers/BaseController` (namespace `SistemaCalificaciones.Controllers`) exposes `IdUsuario`, `Rol`, `IdCentro`, `EsAdministrador`. Non-admin users must only see data from their own centre, filtered by hand in each action (`if (!EsAdministrador && x.CentroId != IdCentro)`). Only some controllers inherit `BaseController` (Asignaciones, Centros, Estudiantes, Maestros, Usuarios); some (e.g. `CursosController`) read the `IdCentro` claim inline. Follow this pattern in any new endpoint that touches centre-owned data.
- **Academic model**: `Nivel` → `Grado` → `Curso` (per `AnioEscolar` and `Centro`). `GradoMateria`/`CursoMateria` link subjects. `AsignacionDocente` (curso + materia + año + maestro) is the unit teachers grade against. Grading works in one of two ways, chosen by `Nivel.UsaCompetencias`:
  - **Activities / learning outcomes** (secondary/polytechnic): `ActividadEvaluativa` → `NotaActividad`, `ResultadoAprendizaje`/`CalificacionRA`, rolled up into `CalificacionPeriodo` by `CalificacionesPeriodoController`.
  - **Competencies** (primary): `Competencia` ↔ `CompetenciaGradoMateria`, `ActividadCompetencia` → `NotaCompetencia`, rolled up into `CalificacionCompetenciaPeriodo`. **Recovery** (`Helpers/RecuperacionHelper`): when a competency's period grade (`Promedio`) is below 70, a recovery grade (`NotaRecuperacion`) is allowed. It can be at most `100 - Promedio`, and `NotaFinal = Promedio + NotaRecuperacion`. `NotaFinal` is `[NotMapped]` and computed in memory, so don't use it inside SQL-translated LINQ. Reports show the original grade in `C*`/`P*`, and the `RC*`/`RP*` columns show `Promedio + NotaRecuperacion` (e.g. 60 + 20 = 80), or null when there was no recovery. Averages use `NotaFinal`.
  `PeriodoPublicacion` defines the grading periods and their publish windows.
- **Reports** (`ReportesController`, the largest file): per-level endpoints (`primaria/`, `secundaria/`, `politecnico/`, period and annual). The PDF endpoint stamps text at fixed coordinates onto `Templates/Secundaria/3ro.pdf` with iText7. The template path is resolved from `Directory.GetCurrentDirectory()` and is not copied to the build output, so the app must run with the project folder as its working directory.
- **Excel import** (`ImportacionExcelController`): ClosedXML bulk upload of students and teachers. It creates `Usuario` accounts through `UsuarioGeneratorService`. Each row is processed on its own: errors are collected per row, and `ChangeTracker.Clear()` runs in `finally` so a failed row's entities aren't retried on the next `SaveChanges`.
- **CORS**: the `ReactPolicy` in `Program.cs` currently whitelists only `http://localhost:5173` (commit `6596a33` removed the Vercel origin `https://sistema-de-calificaciones1.vercel.app`, so the deployed frontend is blocked until a production origin is added back). Origins must not have a trailing slash, or they won't match. Add new frontend deployment URLs there.

`WeatherForecast*` and `UserExtensions.cs` are leftover template/placeholder files.
