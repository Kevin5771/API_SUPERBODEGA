using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace SuperBodegaWeb.Pages.Clientes
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexModel> _logger;

        public List<ClienteDto> Clientes { get; set; } = new();

        [TempData] public string? Mensaje { get; set; }
        [TempData] public string? Error   { get; set; }

        public IndexModel(IHttpClientFactory clientFactory, ILogger<IndexModel> logger)
        {
            _clientFactory = clientFactory;
            _logger        = logger;
        }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            Clientes = await client
                .GetFromJsonAsync<List<ClienteDto>>("api/Clientes")
                ?? new List<ClienteDto>();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            try
            {
                var resp = await client.DeleteAsync($"api/Clientes/{id}");
                if (resp.IsSuccessStatusCode)
                    Mensaje = "✅ Cliente eliminado correctamente.";
                else
                {
                    _logger.LogWarning("Error {Status} al eliminar cliente {Id}", resp.StatusCode, id);
                    Error = "❌ No se pudo eliminar el cliente.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al eliminar cliente {Id}", id);
                Error = "❌ Error al procesar la eliminación.";
            }

            return RedirectToPage();
        }

        // Handler para exportar a Excel
        public async Task<IActionResult> OnGetExportarExcelAsync()
        {
            var client = _clientFactory.CreateClient("SuperBodegaAPI");
            // Llama al endpoint que añadiste en la API: api/Clientes/Export
            var bytes    = await client.GetByteArrayAsync("api/Clientes/Export");
            var fileName = $"Clientes_{DateTime.Today:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        public class ClienteDto
        {
            public int    Id       { get; set; }
            public string Nombre   { get; set; } = string.Empty;
            public string Email    { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
        }
    }
}
