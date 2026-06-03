using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
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
            if (string.IsNullOrWhiteSpace(_options.FromAddress))
                throw new InvalidOperationException("Alamat pengirim email belum dikonfigurasi.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            var socketOptions = GetSocketOptions();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, socketOptions, ct);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                if (string.IsNullOrWhiteSpace(_options.Password))
                    throw new InvalidOperationException("SMTP password belum dikonfigurasi.");

                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }

        private SecureSocketOptions GetSocketOptions()
        {
            if (!_options.EnableSsl)
                return SecureSocketOptions.None;

            return _options.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
        }
    }
}
