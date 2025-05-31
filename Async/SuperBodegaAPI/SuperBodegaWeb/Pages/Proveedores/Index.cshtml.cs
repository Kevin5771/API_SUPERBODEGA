using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace SuperBodegaWeb.Pages.Proveedores
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        private readonly ILogger<IndexModel> _logger;

        public List<ProveedorDto> Proveedores { get; set; } = new();
        [TempData] public string? Mensaje { get; set; }
        [TempData] public string? Error   { get; set; }

        // Propiedad para generar la URL de exportación
        public string ExcelUrl => Url.Page(
            pageName: null,
            pageHandler: "ExportarExcel",
            values: null
        );

        public IndexModel(IHttpClientFactory cf, ILogger<IndexModel> logger)
        {
            _cf     = cf;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.GetAsync("api/Proveedores");
            if (resp.IsSuccessStatusCode)
                Proveedores = await resp.Content.ReadFromJsonAsync<List<ProveedorDto>>() ?? new();
            else
            {
                _logger.LogWarning("Error cargando proveedores (HTTP {Status})", resp.StatusCode);
                Error = $"❌ Error al cargar proveedores (HTTP {(int)resp.StatusCode}).";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var r      = await client.DeleteAsync($"api/Proveedores/{id}");
            if (r.IsSuccessStatusCode)           Mensaje = "✅ Proveedor eliminado.";
            else
            {
                _logger.LogWarning("Error borrando proveedor {Id} (HTTP {Status})", id, r.StatusCode);
                Error = $"❌ No se pudo eliminar (HTTP {(int)r.StatusCode}).";
            }
            return RedirectToPage();
        }

        // Handler para exportar a Excel, igual que en Ventas
        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var bytes = await client.GetByteArrayAsync("api/Proveedores/Export");
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Proveedores.xlsx"
            );
        }

        public class ProveedorDto
        {
            public int    Id       { get; set; }
            public string Nombre   { get; set; } = default!;
            public string Email    { get; set; } = default!;
            public string Telefono { get; set; } = default!;
        }
    }
}
