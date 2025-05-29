using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SuperBodegaWeb.Pages.Ventas
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public IndexModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public List<VentaDto> Ventas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public DateTime FechaFin { get; set; } = DateTime.Today;

        [BindProperty]
        public List<int> Ids { get; set; } = new();

        [BindProperty]
        public List<string> Estados { get; set; } = new();

        public string ExcelUrl => Url.Page(
            pageName: null,
            pageHandler: "ExportarExcel",
            values: new { FechaInicio, FechaFin }
        );

        public async Task OnGetAsync()
        {
            await CargarVentasAsync();
        }

        private async Task CargarVentasAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var url = $"api/Ventas?fechaInicio={FechaInicio:yyyy-MM-dd}&fechaFin={FechaFin:yyyy-MM-dd}";
            var lista = await client.GetFromJsonAsync<List<VentaDto>>(url);
            Ventas = lista ?? new();
        }

        public async Task<IActionResult> OnPostActualizarAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            for (int i = 0; i < Ids.Count; i++)
            {
                var resp = await client.PutAsJsonAsync(
                    $"api/Ventas/CambiarEstado/{Ids[i]}",
                    Estados[i]
                );
                resp.EnsureSuccessStatusCode();
            }

            await CargarVentasAsync();
            return Page();
        }

        // Handler para exportar a Excel (descarga como byte[])
        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var url = $"api/Ventas/Excel?fechaInicio={FechaInicio:yyyy-MM-dd}&fechaFin={FechaFin:yyyy-MM-dd}";

            // Descargar como byte[] para garantizar formato correcto
            var bytes = await client.GetByteArrayAsync(url);
            var fileName = $"Ventas_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public class VentaDto
        {
            public int Id { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public string Producto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public string Estado { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
        }
    }
}
