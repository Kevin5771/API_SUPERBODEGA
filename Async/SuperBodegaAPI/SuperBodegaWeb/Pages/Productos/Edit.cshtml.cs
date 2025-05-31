using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace SuperBodegaWeb.Pages.Productos
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IHttpClientFactory cf, ILogger<EditModel> logger)
        {
            _cf     = cf;
            _logger = logger;
        }

        [BindProperty]
        public ProductInputModel Product { get; set; } = new();

        public List<SelectListItem> Providers { get; set; } = new();

        [TempData] public string? ErrorMessage   { get; set; }
        [TempData] public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.GetAsync($"api/Products/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                ErrorMessage = "❌ Producto no encontrado.";
                return RedirectToPage("Index");
            }

            Product = await resp.Content.ReadFromJsonAsync<ProductInputModel>()!;
            await LoadProvidersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProvidersAsync();
                return Page();
            }

            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.PutAsJsonAsync($"api/Products/{Product.Id}", Product);

            if (resp.IsSuccessStatusCode)
            {
                SuccessMessage = "✅ Producto actualizado correctamente.";
                return RedirectToPage("Index");
            }

            ErrorMessage = $"❌ No se pudo actualizar (HTTP {(int)resp.StatusCode}).";
            await LoadProvidersAsync();
            return Page();
        }

        private async Task LoadProvidersAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var list   = await client.GetFromJsonAsync<List<ProviderDto>>("api/Proveedores");
            Providers = list?
                .Select(x => new SelectListItem(x.Nombre, x.Id.ToString()))
                .ToList()
              ?? new();
        }

        public class ProductInputModel
        {
            public int    Id          { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio")]
            public string Nombre      { get; set; } = "";

            [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero")]
            public decimal Precio     { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número no negativo")]
            public int    Stock       { get; set; }

            [Required(ErrorMessage = "La categoría es obligatoria")]
            public string Categoria   { get; set; } = "";

            public string ImagenUrl   { get; set; } = "";

            [Required(ErrorMessage = "Debes seleccionar un proveedor")]
            public int    ProveedorId { get; set; }
        }

        public class ProviderDto
        {
            public int    Id     { get; set; }
            public string Nombre { get; set; } = "";
        }
    }
}
