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

        public HttpSmsSender(
            IHttpClientFactory httpClientFactory,
            IOptions<SmsSenderOptions> options
        )
        {
            _httpClientFactory = httpClientFactory;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task SendAsync(
            string phoneNumber,
            string message,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(_options.ProviderUrl))
                throw new InvalidOperationException("URL provider SMS belum dikonfigurasi.");
            if (
                _options.ProviderUrl.Contains("example.com", StringComparison.OrdinalIgnoreCase)
                || _options.ProviderUrl.Contains("example.", StringComparison.OrdinalIgnoreCase)
            )
                throw new InvalidOperationException(
                    "URL provider SMS masih contoh/dummy. Isi SmsSender:ProviderUrl dengan endpoint provider SMS/WhatsApp yang asli."
                );
            if (
                string.Equals(
                    _options.ApiKey,
                    "real-provider-api-key",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                throw new InvalidOperationException(
                    "API key SMS masih placeholder. Isi SmsSender:ApiKey dengan key provider yang asli."
                );

            var client = _httpClientFactory.CreateClient("SmsProvider");

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ProviderUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    _options.ApiKey
                );
            }

            var payload = new
            {
                to = phoneNumber,
                from = _options.SenderName,
                message,
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
