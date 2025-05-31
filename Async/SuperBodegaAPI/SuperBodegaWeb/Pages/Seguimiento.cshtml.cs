using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SuperBodegaWeb.Pages
{
    public class SeguimientoModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SeguimientoModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string Codigo { get; set; } = string.Empty;

        public SeguimientoVentaDto? Resultado { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Codigo))
                return;

            var client = _httpClientFactory.CreateClient("SuperBodegaAPI");
            try
            {
                Resultado = await client.GetFromJsonAsync<SeguimientoVentaDto>(
                    $"api/Ventas/Seguimiento/{Codigo}"
                );
            }
            catch
            {
                Resultado = null;
            }
        }

        public class SeguimientoVentaDto
        {
            public string Cliente { get; set; } = string.Empty;
            public DateTime FechaVenta { get; set; }
            public string EstadoActual { get; set; } = string.Empty;
            public string CodigoSeguimiento { get; set; } = string.Empty;

            // Lista de productos vendidos
            public List<VentaItemDto> Items { get; set; } = new();

            // Historial de cambios de estado
            public List<CambioEstadoDto> Cambios { get; set; } = new();
        }

        public class VentaItemDto
        {
            public string NombreProducto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        public class CambioEstadoDto
        {
            public string EstadoAnterior { get; set; } = string.Empty;
            public string EstadoNuevo { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
        }
    }
}
