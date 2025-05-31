using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SuperBodegaWeb.Pages.Clientes
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _cf;
        private readonly ILogger<EditModel> _logger;
        public EditModel(IHttpClientFactory cf, ILogger<EditModel> logger)
        {
            _cf = cf;
            _logger = logger;
        }

        [BindProperty]
        public ClienteDto Cliente { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            Cliente = await client.GetFromJsonAsync<ClienteDto>($"api/Clientes/{id}")
                      ?? new ClienteDto();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _cf.CreateClient("SuperBodegaAPI");
            var response = await client.PutAsJsonAsync(
                $"api/Clientes/{Cliente.Id}", Cliente);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Status {status} al actualizar cliente {id}",
                                   response.StatusCode, Cliente.Id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo actualizar el cliente: " +
                    await response.Content.ReadAsStringAsync());
                return Page();
            }

            TempData["Mensaje"] = "Cliente actualizado correctamente.";
            return RedirectToPage("Index");
        }

        public class ClienteDto
        {
            public int Id { get; set; }
            public string Nombre  { get; set; } = string.Empty;
            public string Email   { get; set; } = string.Empty;
            public string Telefono{ get; set; } = string.Empty;
        }
    }
}