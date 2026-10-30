using Microsoft.EntityFrameworkCore;

namespace SistemaCalificaciones.Helpers
{
    public class ResultadoPaginado<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    public static class Paginacion
    {
        public const int TamanoMaximo = 100;

        // La consulta debe venir ordenada para que las páginas sean estables
        public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
            this IQueryable<T> query,
            int pagina,
            int tamanoPagina)
        {
            tamanoPagina = Math.Clamp(tamanoPagina, 1, TamanoMaximo);

            var total = await query.CountAsync();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);

            var items = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            return new ResultadoPaginado<T>
            {
                Items = items,
                Total = total,
                Pagina = pagina,
                TamanoPagina = tamanoPagina,
                TotalPaginas = totalPaginas
            };
        }
    }
}
