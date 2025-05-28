using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace SuperBodegaWeb.Pages.Clientes
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        public CreateModel(IHttpClientFactory cf) => _cf = cf;

        [BindProperty]
        public ClienteDto Cliente { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var res = await client.PostAsJsonAsync("api/Clientes", Cliente);
            if (res.IsSuccessStatusCode)
                return RedirectToPage("Index");

            ModelState.AddModelError(string.Empty, "Error al crear cliente.");
            return Page();
        }

        public class ClienteDto
        {
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
        }
    }
}