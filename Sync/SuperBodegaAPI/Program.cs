using System;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SuperBodegaAPI.Data;

namespace SuperBodegaAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1) Cadena de conexión
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 2) DbContext con retry
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    )
                )
            );

            // 3) CORS — permitir cualquier origen, método y encabezado
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                );
            });

            // 4) Servicios y Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SuperBodegaAPI",
                    Version = "v1",
                    Description = "API para gestionar SuperBodega",
                });
            });

            // 5) Forzar Kestrel a escuchar en 0.0.0.0:80
            builder.WebHost.UseUrls("http://0.0.0.0:80");

            var app = builder.Build();

            // 6) Migración automática con espera
            var maxAttempts = 10;
            var delay = TimeSpan.FromSeconds(5);
            var connected = false;

            for (int i = 1; i <= maxAttempts && !connected; i++)
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                    Console.WriteLine("✅ Migraciones aplicadas y base de datos lista.");
                    connected = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⏳ Intento {i}/{maxAttempts}: Esperando conexión a SQL Server...");
                    Console.WriteLine($"Error: {ex.Message}");
                    Thread.Sleep(delay);
                }
            }

            if (!connected)
            {
                Console.WriteLine("❌ No se pudo conectar a la base de datos.");
                return;
            }

            // 7) Middleware pipeline

            // Permitir CORS
            app.UseCors("AllowAll");

            // Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SuperBodegaAPI v1");
                c.RoutePrefix = "swagger";
            });

            // Autorización (si en el futuro añades [Authorize])
            app.UseAuthorization();

            // Mapear controladores
            app.MapControllers();

            // Ruta raíz rápida
            app.MapGet("/", () => "🚀 ¡Bienvenido a SuperBodegaAPI!");

            app.Run();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
