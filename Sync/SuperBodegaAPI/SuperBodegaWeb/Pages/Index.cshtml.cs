// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

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

        public IndexModel(IHttpClientFactory httpFactory, ILogger<IndexModel> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            if (!EsAdmin) return;

            var client = _httpFactory.CreateClient("SuperBodegaAPI");
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
    }
}
