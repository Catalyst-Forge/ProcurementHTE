using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;
        private readonly EmailSenderOptions _options;

        public ConsoleEmailSender(IOptions<EmailSenderOptions> options, ILogger<ConsoleEmailSender> logger)
        {
            _options = options?.Value ?? new EmailSenderOptions();
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            _logger.LogInformation(
                """
                [EMAIL OUT]
                From   : {FromName} <{FromAddress}>
                To     : {ToEmail}
                Subject: {Subject}
                Body   : {Body}
                """,
                _options.FromName,
                _options.FromAddress,
                toEmail,
                subject,
                htmlBody
            );
            return Task.CompletedTask;
        }
    }
}
