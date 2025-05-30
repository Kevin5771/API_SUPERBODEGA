// Carrito.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SuperBodegaWeb.Pages
{
    [Authorize(Roles = "Cliente,Admin")]
    public class CarritoModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly FacturaService _facturaService;
        private readonly ILogger<CarritoModel> _logger;

        public List<CarritoItemDto> Items { get; set; } = new();
        public decimal Total => Items.Sum(p => p.Subtotal);
        public string? Mensaje { get; set; }

        public CarritoModel(IHttpClientFactory clientFactory, FacturaService facturaService, ILogger<CarritoModel> logger)
        {
            _clientFactory = clientFactory;
            _facturaService = facturaService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email is null) return;

            var client = _clientFactory.CreateClient("SuperBodegaAPI");

            try
            {
                var cliente = await client.GetFromJsonAsync<ClienteDto>($"api/Clientes/PorEmail/{email}");
                if (cliente is null) return;

                var resItems = await client.GetFromJsonAsync<List<CarritoItemDto>>($"api/Carrito/DelCliente/{cliente.Id}");
                if (resItems is not null) Items = resItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el carrito.");
            }
        }

        public async Task<IActionResult> OnPostActualizarCantidadAsync(int ProductoId, int Cantidad)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email is null) return BadRequest();

            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var cliente = await client.GetFromJsonAsync<ClienteDto>($"api/Clientes/PorEmail/{email}");
            if (cliente is null) return NotFound();

            var data = new
            {
                ClienteId = cliente.Id,
                ProductoId = ProductoId,
                Cantidad = Cantidad
            };

            var response = await client.PostAsJsonAsync("api/Carrito/ActualizarCantidad", data);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int ProductoId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email is null) return BadRequest();

            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var cliente = await client.GetFromJsonAsync<ClienteDto>($"api/Clientes/PorEmail/{email}");
            if (cliente is null) return NotFound();

            var response = await client.DeleteAsync($"api/Carrito/{cliente.Id}/{ProductoId}");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostComprarAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email is null) return BadRequest();

            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            var cliente = await client.GetFromJsonAsync<ClienteDto>($"api/Clientes/PorEmail/{email}");
            if (cliente is null) return NotFound();

            var items = await client.GetFromJsonAsync<List<CarritoItemDto>>($"api/Carrito/DelCliente/{cliente.Id}");
            if (items == null || !items.Any()) return BadRequest("No hay productos en el carrito.");

            var response = await client.PostAsync($"api/Carrito/Comprar/{cliente.Id}", null);
            if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

            var pdf = _facturaService.GenerarFactura(cliente, items, items.Sum(i => i.Subtotal), "Recibido");
            return File(pdf, "application/pdf", $"Factura_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        public class ClienteDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
        }

        public class CarritoItemDto
        {
            public int ProductoId { get; set; }
            public string NombreProducto { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public decimal Subtotal => Precio * Cantidad;
            public string ImagenUrl { get; set; } = "/images/placeholder.png";
        }
    }
}
