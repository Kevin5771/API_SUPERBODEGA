using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

        // Ahora incluye también la lista de items de cada venta
        public List<VentaDto> Ventas { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public DateTime FechaFin { get; set; } = DateTime.Today;

        [BindProperty]
        public List<int> Ids { get; set; } = new();

        [BindProperty]
        public List<string> Estados { get; set; } = new();

        public string? ErrorMensaje { get; set; }

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

            // Para cada venta, traemos también el detalle de items
            foreach (var venta in Ventas)
            {
                var seguimiento = await client.GetFromJsonAsync<SeguimientoVentaDto>(
                    $"api/Ventas/Seguimiento/{venta.CodigoSeguimiento}"
                );
                venta.Items = seguimiento?.Items ?? new List<VentaItemDto>();
            }
        }

        public async Task<IActionResult> OnPostActualizarAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");

            for (int i = 0; i < Ids.Count; i++)
            {
                var jsonString = $"\"{Estados[i]}\"";
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/Ventas/CambiarEstado/{Ids[i]}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var mensaje = await response.Content.ReadAsStringAsync();
                    ErrorMensaje = $"Error al actualizar venta #{Ids[i]}: {mensaje}";
                    await CargarVentasAsync();
                    return Page();
                }
            }

            await CargarVentasAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var url = $"api/Ventas/Excel?fechaInicio={FechaInicio:yyyy-MM-dd}&fechaFin={FechaFin:yyyy-MM-dd}";

            var bytes = await client.GetByteArrayAsync(url);
            var fileName = $"Ventas_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // DTO local ajustado al nuevo VentaAdminDto e incluye la lista de items
        public class VentaDto
        {
            public int Id { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
            public int ItemsCount { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string CodigoSeguimiento { get; set; } = string.Empty;

            // LISTA DE ITEMS VENDIDOS
            public List<VentaItemDto> Items { get; set; } = new();
        }

        public class VentaItemDto
        {
            public string NombreProducto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        // Para mapear la respuesta del endpoint de seguimiento
        private class SeguimientoVentaDto
        {
            public List<VentaItemDto>? Items { get; set; }
        }
    }
}
