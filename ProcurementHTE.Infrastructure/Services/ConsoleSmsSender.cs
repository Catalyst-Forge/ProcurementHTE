using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public class ConsoleSmsSender : ISmsSender
    {
        private readonly ILogger<ConsoleSmsSender> _logger;
        private readonly SmsSenderOptions _options;

        public ConsoleSmsSender(IOptions<SmsSenderOptions> options, ILogger<ConsoleSmsSender> logger)
        {
            _options = options?.Value ?? new SmsSenderOptions();
            _logger = logger;
        }

        public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            _logger.LogInformation(
                """
                [SMS OUT]
                From : {Sender}
                To   : {Phone}
                Body : {Body}
                """,
                _options.SenderName,
                phoneNumber,
                message
            );
            return Task.CompletedTask;
        }
    }
}
