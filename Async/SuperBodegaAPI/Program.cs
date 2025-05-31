using System;
using System.Threading;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SuperBodegaAPI.Consumers;
using SuperBodegaAPI.Data;
using SuperBodegaAPI.Services;
using SuperBodegaAPI.Settings;

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
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
                )
            );

            // 3) CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                );
            });

            // 4) MassTransit + RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<EstadoPedidoActualizadoConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h =>
                    {
                        h.Username("admin");
                        h.Password("admin");
                    });

                    cfg.ReceiveEndpoint("notificaciones-estado-pedido", e =>
                    {
                        e.ConfigureConsumer<EstadoPedidoActualizadoConsumer>(context);
                    });
                });
            });

            // 5) Configuración del servicio de correos SMTP
            builder.Services.Configure<SmtpSettings>(
                builder.Configuration.GetSection("Smtp")
            );
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<CorreoQueue>();
            builder.Services.AddHostedService<CorreoBackgroundService>();

            // 6) Controladores y Swagger
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

            // 7) Escuchar en el puerto 80
            builder.WebHost.UseUrls("http://0.0.0.0:80");

            var app = builder.Build();

            // 8) Migración automática con retry
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

            // 9) Middleware
            app.UseCors("AllowAll");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SuperBodegaAPI v1");
                c.RoutePrefix = "swagger";
            });

            app.UseAuthorization();

            app.MapControllers();
            app.MapGet("/", () => "¡Bienvenido a SuperBodegaAPI!");

            app.Run();
        }
    }
}
