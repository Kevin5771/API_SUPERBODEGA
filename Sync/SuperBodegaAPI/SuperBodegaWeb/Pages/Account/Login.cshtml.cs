using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace SuperBodegaWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IHttpClientFactory clientFactory, ILogger<LoginModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        [BindProperty, Required, EmailAddress]
        public string Username { get; set; } = string.Empty;

        [BindProperty, Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var client = _clientFactory.CreateClient("SuperBodegaAPI");

            try
            {
                var loginRes = await client.PostAsJsonAsync("api/Usuarios/Login", new
                {
                    Email = Username,
                    Password = Password
                });

                if (!loginRes.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
                    return Page();
                }

                var user = await loginRes.Content.ReadFromJsonAsync<UsuarioDto>();
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Error al procesar datos de usuario.");
                    return Page();
                }

                // Verificar o crear cliente
                var clienteRes = await client.GetAsync($"api/Clientes/PorEmail/{user.Email}");
                if (!clienteRes.IsSuccessStatusCode)
                {
                    var crearRes = await client.PostAsJsonAsync("api/Clientes", new
                    {
                        Nombre = user.Nombre,
                        Email = user.Email,
                        Telefono = "00000000"
                    });
                    if (!crearRes.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError(string.Empty, "Error al registrar cliente.");
                        return Page();
                    }
                }

                // Crear claims e iniciar sesión
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.Nombre),
                    new Claim(ClaimTypes.Role, user.Rol)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                _logger.LogInformation("Usuario {Email} inició sesión.", user.Email);
                return LocalRedirect(ReturnUrl ?? "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el proceso de login para {Email}.", Username);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al procesar tu solicitud.");
                return Page();
            }
        }

        public class UsuarioDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
        }
    }
}
