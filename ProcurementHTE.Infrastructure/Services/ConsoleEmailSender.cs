using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Services
{
    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default
        )
        {
            return Task.CompletedTask;
        }
    }
}
