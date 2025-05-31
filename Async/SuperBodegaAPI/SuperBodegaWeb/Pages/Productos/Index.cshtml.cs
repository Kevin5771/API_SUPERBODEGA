using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SuperBodegaWeb.Pages.Productos
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        private readonly ILogger<IndexModel> _logger;

        public List<ProductoDto> Productos { get; set; } = new();
        [TempData] public string? Mensaje { get; set; }
        [TempData] public string? Error   { get; set; }

        public IndexModel(IHttpClientFactory cf, ILogger<IndexModel> logger)
        {
            _cf     = cf;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.GetAsync("api/Products/Catalogo");
            if (resp.IsSuccessStatusCode)
            {
                var dto = await resp.Content.ReadFromJsonAsync<CatalogoResponse>();
                Productos = dto?.Productos ?? new();
            }
            else
            {
                _logger.LogWarning("Error al cargar catálogo (HTTP {Status})", resp.StatusCode);
                Error = $"❌ Error al cargar catálogo (HTTP {(int)resp.StatusCode}).";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var r      = await client.DeleteAsync($"api/Products/{id}");
            if (r.IsSuccessStatusCode)
                Mensaje = "✅ Producto eliminado.";
            else
            {
                _logger.LogWarning("Error borrando producto {Id} (HTTP {Status})", id, r.StatusCode);
                Error = $"❌ No se pudo eliminar (HTTP {(int)r.StatusCode}).";
            }
            return RedirectToPage();
        }

        // Handler para exportar a Excel
        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var client   = _cf.CreateClient("SuperBodegaAPI");
            var bytes    = await client.GetByteArrayAsync("api/Products/Export");
            var fileName = $"Productos_{DateTime.Today:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public class ProductoDto
        {
            public int     Id        { get; set; }
            public string  Nombre    { get; set; } = default!;
            public decimal Precio    { get; set; }
            public int     Stock     { get; set; }
            public string  Categoria { get; set; } = default!;
            public string  Proveedor { get; set; } = default!;
        }

        public class CatalogoResponse
        {
            public List<ProductoDto> Productos { get; set; } = new();
        }
    }
}
