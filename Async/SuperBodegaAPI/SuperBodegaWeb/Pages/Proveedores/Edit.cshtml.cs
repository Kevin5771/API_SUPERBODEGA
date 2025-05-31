using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SuperBodegaWeb.Pages.Proveedores
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _cf;

        public EditModel(IHttpClientFactory cf) => _cf = cf;

        [BindProperty]
        public ProveedorDto Proveedor { get; set; } = new();

        [TempData] public string? Mensaje { get; set; }
        [TempData] public string? Error   { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.GetAsync($"api/Proveedores/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                Error = $"❌ No se pudo cargar (HTTP {(int)resp.StatusCode}).";
                return RedirectToPage("Index");
            }
            Proveedor = await resp.Content.ReadFromJsonAsync<ProveedorDto>()!;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var resp   = await client.PutAsJsonAsync($"api/Proveedores/{Proveedor.Id}", Proveedor);
            if (resp.IsSuccessStatusCode)
            {
                Mensaje = "✅ Proveedor actualizado.";
                return RedirectToPage("Index");
            }
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                Error = "❌ Proveedor no encontrado.";
            else
                Error = $"❌ No se pudo actualizar (HTTP {(int)resp.StatusCode}).";

            return Page();
        }

        public class ProveedorDto
        {
            public int    Id       { get; set; }
            [Required] public string Nombre   { get; set; } = default!;
            [Required, EmailAddress]
                         public string Email    { get; set; } = default!;
            [Required] public string Telefono { get; set; } = default!;
        }
    }
}
