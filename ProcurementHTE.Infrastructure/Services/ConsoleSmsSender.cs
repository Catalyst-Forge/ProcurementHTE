using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Services
{
    public class ConsoleSmsSender : ISmsSender
    {
        public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
