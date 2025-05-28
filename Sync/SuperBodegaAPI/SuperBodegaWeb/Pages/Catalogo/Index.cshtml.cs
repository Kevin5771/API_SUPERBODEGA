using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SuperBodegaWeb.Pages.Catalogo
{
    [Authorize(Roles = "Admin,Cliente")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexModel> _logger;

        public List<ProductoDto> Productos { get; set; } = new();

        public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            try
            {
                var response = await client.GetFromJsonAsync<CatalogoResponse>("api/Products/Catalogo");
                if (response?.Productos != null)
                {
                    Productos = response.Productos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener catálogo");
            }
        }

        public async Task<IActionResult> OnPostAgregarAlCarritoAsync(int productoId, int cantidad)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                TempData["Mensaje"] = "Usuario no autenticado.";
                return RedirectToPage();
            }

            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            try
            {
                var clienteRes = await client.GetAsync($"api/Clientes/PorEmail/{email}");
                if (!clienteRes.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Cliente no encontrado.";
                    return RedirectToPage();
                }

                var cliente = await clienteRes.Content.ReadFromJsonAsync<ClienteDto>();
                var item = new { ClienteId = cliente!.Id, ProductoId = productoId, Cantidad = cantidad };

                var cartRes = await client.PostAsJsonAsync("api/Carrito", item);
                TempData["Mensaje"] = cartRes.IsSuccessStatusCode
                    ? "✅ Producto agregado al carrito."
                    : "❌ Error al agregar producto.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar al carrito");
                TempData["Mensaje"] = "❌ Error al procesar la solicitud.";
            }

            return RedirectToPage();
        }

        public class ProductoDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public decimal Precio { get; set; }
            public int Stock { get; set; }
            public string Categoria { get; set; } = string.Empty;
            public string Proveedor { get; set; } = string.Empty;
            public string ImagenUrl { get; set; } = string.Empty;
        }

        public class CatalogoResponse
        {
            public int Total { get; set; }
            public List<ProductoDto> Productos { get; set; } = new();
        }

        public class ClienteDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
