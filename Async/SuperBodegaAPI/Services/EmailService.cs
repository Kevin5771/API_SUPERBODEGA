// Services/EmailService.cs
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SuperBodegaAPI.Settings;

namespace SuperBodegaAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;

        public EmailService(IOptions<SmtpSettings> cfg)
        {
            _smtp = cfg.Value;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(_smtp.User));
            msg.To.Add(MailboxAddress.Parse(destinatario));
            msg.Subject = asunto;
            msg.Body = new TextPart("html") { Text = cuerpoHtml };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtp.Host, _smtp.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_smtp.User, _smtp.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
    }
}
