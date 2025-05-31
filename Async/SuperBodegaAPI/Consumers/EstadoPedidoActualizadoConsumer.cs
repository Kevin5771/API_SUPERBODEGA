using MassTransit;
using Microsoft.Extensions.Logging;
using SuperBodegaAPI.Events;
using SuperBodegaAPI.Services;

namespace SuperBodegaAPI.Consumers
{
    public class EstadoPedidoActualizadoConsumer : IConsumer<EstadoPedidoActualizado>
    {
        private readonly ILogger<EstadoPedidoActualizadoConsumer> _logger;
        private readonly CorreoQueue _correoQueue;

        public EstadoPedidoActualizadoConsumer(
            ILogger<EstadoPedidoActualizadoConsumer> logger,
            CorreoQueue correoQueue)
        {
            _logger = logger;
            _correoQueue = correoQueue;
        }

        public Task Consume(ConsumeContext<EstadoPedidoActualizado> context)
        {
            var msg = context.Message;

            _logger.LogInformation("📧 Enviando notificación de estado para pedido {TrackingCode}: {NuevoEstado}",
                msg.CodigoSeguimiento, msg.NuevoEstado);

            var cuerpo = $@"
<div style='font-family:Segoe UI, sans-serif; max-width:600px; margin:auto; border:1px solid #e0e0e0; border-radius:8px; overflow:hidden;'>
    <div style='background-color:#0d6efd; color:#fff; padding:20px; text-align:center;'>
        <h1 style='margin:0;'>SuperBodega</h1>
        <p style='margin:0;'>Actualización de Estado de Pedido</p>
    </div>
    <div style='padding:30px; background-color:#f9f9f9;'>
        <p>Hola estimado cliente,</p>
        <p>Queremos informarte que el estado de tu pedido con código:</p>
        <p style='font-size:1.2em; font-weight:bold; color:#0d6efd;'>{msg.CodigoSeguimiento}</p>
        <p>ha cambiado a:</p>
        <p style='font-size:1.4em; font-weight:bold; color:#198754;'>{msg.NuevoEstado}</p>

        <div style='margin: 20px 0; text-align: center;'>
            <a href='https://superbodega.com/seguimiento?codigo={msg.CodigoSeguimiento}'
               style='display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                Ver seguimiento
            </a>
        </div>

        <hr style='margin:30px 0;' />
        <p style='font-size:0.9em; color:#6c757d;'>Gracias por confiar en nosotros.</p>
        <p style='font-size:0.9em; color:#6c757d;'>SuperBodega - Tu tienda de confianza.</p>
    </div>
    <div style='background-color:#f1f1f1; padding:15px; text-align:center; font-size:0.8em; color:#888;'>
        © 2025 SuperBodega. Todos los derechos reservados.
    </div>
</div>";

            // Llamada optimizada
            _correoQueue.Enqueue(
                msg.EmailCliente,
                $"Su pedido {msg.CodigoSeguimiento} está {msg.NuevoEstado}",
                cuerpo
            );

            return Task.CompletedTask;
        }
    }
}
