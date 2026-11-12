-- Recuperación por competencias (materias que usan competencias)
-- Agrega la nota de recuperación a CalificacionesCompetenciasPeriodo.
-- Es seguro ejecutarlo más de una vez: solo agrega las columnas si no existen.

IF COL_LENGTH('dbo.CalificacionesCompetenciasPeriodo', 'NotaRecuperacion') IS NULL
    ALTER TABLE dbo.CalificacionesCompetenciasPeriodo ADD NotaRecuperacion decimal(5,2) NULL;

IF COL_LENGTH('dbo.CalificacionesCompetenciasPeriodo', 'FechaRecuperacion') IS NULL
    ALTER TABLE dbo.CalificacionesCompetenciasPeriodo ADD FechaRecuperacion datetime2 NULL;
