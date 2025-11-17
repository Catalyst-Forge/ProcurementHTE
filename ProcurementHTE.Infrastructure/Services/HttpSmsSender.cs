using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public class HttpSmsSender : ISmsSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SmsSenderOptions _options;
        private readonly ILogger<HttpSmsSender> _logger;

        public HttpSmsSender(
            IHttpClientFactory httpClientFactory,
            IOptions<SmsSenderOptions> options,
            ILogger<HttpSmsSender> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public async Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ProviderUrl))
                throw new InvalidOperationException("URL provider SMS belum dikonfigurasi.");

            var client = _httpClientFactory.CreateClient("SmsProvider");

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ProviderUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            var payload = new
            {
                to = phoneNumber,
                from = _options.SenderName,
                message
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("SMS dikirim ke {Phone}", phoneNumber);
        }
    }
}
