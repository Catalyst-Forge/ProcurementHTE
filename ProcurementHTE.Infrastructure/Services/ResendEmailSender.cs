using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public sealed class ResendEmailSender : IEmailSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailSenderOptions _options;

        public ResendEmailSender(
            IHttpClientFactory httpClientFactory,
            IOptions<EmailSenderOptions> options
        )
        {
            _httpClientFactory = httpClientFactory;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Resend API key belum dikonfigurasi.");

            if (string.IsNullOrWhiteSpace(_options.FromAddress))
                throw new InvalidOperationException("Alamat pengirim email belum dikonfigurasi.");

            var endpoint = string.IsNullOrWhiteSpace(_options.ApiUrl)
                ? "https://api.resend.com/emails"
                : _options.ApiUrl;

            var from = string.IsNullOrWhiteSpace(_options.FromName)
                ? _options.FromAddress
                : $"{_options.FromName} <{_options.FromAddress}>";

            var payload = new
            {
                from,
                to = new[] { toEmail },
                subject,
                html = htmlBody,
            };

            var client = _httpClientFactory.CreateClient("Resend");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Resend gagal mengirim email ({(int)response.StatusCode} {response.ReasonPhrase}): {responseBody}"
            );
        }
    }
}
