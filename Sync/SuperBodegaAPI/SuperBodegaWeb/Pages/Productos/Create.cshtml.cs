using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace SuperBodegaWeb.Pages.Productos
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        private readonly ILogger<CreateModel> _logger;
        private readonly IWebHostEnvironment _env;

        public CreateModel(IHttpClientFactory cf, ILogger<CreateModel> logger, IWebHostEnvironment env)
        {
            _cf = cf;
            _logger = logger;
            _env = env;
        }

        [BindProperty]
        public ProductoInputModel Producto { get; set; } = new();

        public List<SelectListItem> Proveedores { get; set; } = new();

        [TempData] public string? Mensaje { get; set; }
        [TempData] public string? Error { get; set; }

        public async Task OnGetAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var lista = await client.GetFromJsonAsync<List<ProveedorDto>>("api/Proveedores");
            Proveedores = lista?
                .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
                .ToList()
              ?? new();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // 1. Guardar imagen localmente
            if (Producto.Imagen is { Length: > 0 })
            {
                var ext = Path.GetExtension(Producto.Imagen.FileName);
                var nombreArchivo = $"{Guid.NewGuid()}{ext}";
                var rutaCarpeta = Path.Combine(_env.WebRootPath, "images", "productos");

                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);

                var rutaFinal = Path.Combine(rutaCarpeta, nombreArchivo);
                using var stream = new FileStream(rutaFinal, FileMode.Create);
                await Producto.Imagen.CopyToAsync(stream);

                Producto.ImagenUrl = $"/images/productos/{nombreArchivo}";
            }

            // 2. Enviar a API como JSON plano
            var client = _cf.CreateClient("SuperBodegaAPI");

            var jsonBody = new
            {
                Producto.Nombre,
                Producto.Precio,
                Producto.Stock,
                Producto.Categoria,
                ImagenUrl = Producto.ImagenUrl,
                Producto.ProveedorId
            };

            var response = await client.PostAsJsonAsync("api/Products", jsonBody);

            if (response.IsSuccessStatusCode)
            {
                Mensaje = "✅ Producto creado correctamente.";
                return RedirectToPage("Index");
            }

            _logger.LogWarning("Error creando producto (HTTP {Status})", response.StatusCode);
            Error = $"❌ No se pudo crear (HTTP {(int)response.StatusCode}).";
            await OnGetAsync();
            return Page();
        }

        public class ProductoInputModel
        {
            [Required] public string Nombre { get; set; } = "";
            [Range(0.01, double.MaxValue)] public decimal Precio { get; set; }
            [Range(0, int.MaxValue)] public int Stock { get; set; }
            [Required] public string Categoria { get; set; } = "";
            public IFormFile? Imagen { get; set; }
            public string ImagenUrl { get; set; } = "";
            [Required] public int ProveedorId { get; set; }
        }

        public class ProveedorDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
        }
    }
}
