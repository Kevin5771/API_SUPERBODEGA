using Microsoft.AspNetCore.Authentication.Cookies;
using SuperBodegaWeb.Pages; // FacturaService
using System;

var builder = WebApplication.CreateBuilder(args);

// 1) Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2) Leer la URL base de la API desde configuración
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
Console.WriteLine($"➡️ API Base URL: {apiBaseUrl}");

// 3) Configurar autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// 4) Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// 5) Cliente HTTP para consumir la API
builder.Services.AddHttpClient("SuperBodegaAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// 6) Otros servicios necesarios
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<FacturaService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// 7) Manejo de errores
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 8) Archivos estáticos (asegura el acceso a /wwwroot/images)
app.UseStaticFiles();

// 9) Middleware de enrutamiento y seguridad
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 10) Rutas de Razor Pages
app.MapRazorPages();

Console.WriteLine("🟢 SuperBodegaWeb está en marcha.");
app.Run();
