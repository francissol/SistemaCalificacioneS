namespace SistemaCalificaciones.Helpers
{
    // Regla de recuperación para materias por competencias:
    // si la nota de una competencia en un período es menor que 70, se abre la recuperación,
    // que vale lo que le falta a la nota para llegar a 100 (ej.: 55 → la recuperación vale hasta 45).
    // La nota final de la competencia es la nota original más la recuperación, sin pasar de 100.
    public static class RecuperacionHelper
    {
        public const decimal NotaMinimaAprobacion = 70m;
        public const decimal NotaMaxima = 100m;

        public static bool RequiereRecuperacion(decimal nota) => nota < NotaMinimaAprobacion;

        public static decimal ValorMaximoRecuperacion(decimal nota) =>
            RequiereRecuperacion(nota) ? NotaMaxima - nota : 0m;

        public static bool RecuperacionValida(decimal nota, decimal recuperacion) =>
            RequiereRecuperacion(nota) && recuperacion >= 0 && recuperacion <= ValorMaximoRecuperacion(nota);

        public static decimal NotaFinal(decimal nota, decimal? recuperacion) =>
            Math.Min(NotaMaxima, nota + (recuperacion ?? 0m));
    }
}
