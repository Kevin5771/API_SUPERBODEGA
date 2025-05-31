using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SuperBodegaAPI.Services
{
    public class CorreoBackgroundService : BackgroundService
    {
        private readonly CorreoQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CorreoBackgroundService> _logger;

        public CorreoBackgroundService(CorreoQueue queue, IServiceScopeFactory scopeFactory, ILogger<CorreoBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (to, subject, html) = await _queue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                try
                {
                    await emailService.EnviarCorreoAsync(to, subject, html);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al enviar correo a {to}", to);
                }
            }
        }
    }
}
