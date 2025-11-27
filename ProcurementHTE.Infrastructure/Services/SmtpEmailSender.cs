using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSenderOptions _options;

        public SmtpEmailSender(IOptions<EmailSenderOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
                throw new InvalidOperationException("SMTP host belum dikonfigurasi.");

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            await client.SendMailAsync(message, ct);
        }
    }
}
