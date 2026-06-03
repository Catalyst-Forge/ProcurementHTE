using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Infrastructure.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private static readonly Uri SendEmailUri = new("https://api.resend.com/emails");
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
                throw new InvalidOperationException("Email pengirim belum dikonfigurasi.");

            var client = _httpClientFactory.CreateClient("Resend");

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEmailUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
