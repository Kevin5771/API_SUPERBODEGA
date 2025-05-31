using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using SuperBodegaWeb.Pages;

namespace SuperBodegaWeb.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<IndexModel> _logger;

        public int TotalProductos { get; set; }
        public int TotalClientes  { get; set; }
        public int TotalVentas    { get; set; }

        public bool EsAdmin => User.IsInRole("Admin");

        public SeguimientoModel.SeguimientoVentaDto? UltimaVenta { get; set; }

        public IndexModel(IHttpClientFactory httpFactory, ILogger<IndexModel> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var client = _httpFactory.CreateClient("SuperBodegaAPI");

            if (EsAdmin)
            {
                try
                {
                    TotalProductos = await client.GetFromJsonAsync<int>("api/Products/Count");
                    TotalClientes  = await client.GetFromJsonAsync<int>("api/Clientes/Count");
                    TotalVentas    = await client.GetFromJsonAsync<int>("api/Ventas/Count");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener contadores del dashboard");
                }
            }
            else
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    try
                    {
                        UltimaVenta = await client.GetFromJsonAsync<SeguimientoModel.SeguimientoVentaDto>($"api/Ventas/UltimaVentaPorCliente/{email}");
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Última venta no encontrada para el cliente.");
                    }
                }
            }
        }
    }
}
