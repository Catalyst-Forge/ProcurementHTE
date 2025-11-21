using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class DocumentGenerator : IDocumentGenerator
    {
        private readonly ITemplateProvider _templateProvider;
        private readonly IHtmlTokenReplacer _tokenReplacer;
        private readonly ILogger<DocumentGenerator> _logger;
        private readonly IProcurementRepository _ProcurementRepository;
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IVendorRepository _vendorRepository;

        public DocumentGenerator(
            ITemplateProvider templateProvider,
            IHtmlTokenReplacer tokenReplacer,
            ILogger<DocumentGenerator> logger,
            IProcurementRepository ProcurementRepository,
            IProfitLossRepository pnlRepository,
            IVendorRepository vendorRepository
        )
        {
            _templateProvider = templateProvider;
            _tokenReplacer = tokenReplacer;
            _logger = logger;
            _ProcurementRepository = ProcurementRepository;
            _pnlRepository = pnlRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<byte[]> GenerateProfitLossAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("ProfitLoss", "Profit & Loss", procurement, ct);

        }

        public async Task<byte[]> GenerateMemorandumAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("Memorandum", "Memorandum", procurement, ct);
        }

        public async Task<byte[]> GeneratePermintaanPekerjaanAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "PermintaanPekerjaan",
                "Permintaan Pekerjaan",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateServiceOrderAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("ServiceOrder", "Service Order", procurement, ct);
        }

        public async Task<byte[]> GenerateMarketSurveyAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            var template = await _templateProvider.GetTemplateAsync("MarketSurvey", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, procurement, ct);
            var pnl = await _pnlRepository.GetLatestByProcurementIdAsync(procurement.ProcurementId);
            if (pnl != null && !string.IsNullOrWhiteSpace(pnl.SelectedVendorId))
            {
                var selectedVendor = await _vendorRepository.GetByIdAsync(pnl.SelectedVendorId);
                html = html.Replace("{{SelectedVendorName}}", selectedVendor?.VendorName ?? "-");
                html = html.Replace(
                    "{{SelectedVendorPrice}}",
                    pnl.SelectedVendorFinalOffer.ToString("N0")
                );
            }

            return await HtmlToPdfAsync(html, "Market Survey", ct);
        }

        public async Task<byte[]> GenerateSPMPAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SPMP",
                "Surat Perintah Mulai Pekerjaan",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateSuratPenawaranHargaAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SuratPenawaranHarga",
                "Surat Penawaran Harga",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateSuratNegosiasiHargaAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SuratNegosiasiHarga",
                "Surat Negosiasi Harga",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateRKSAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "RKS",
                "Rencana Kerja dan Syarat-Syarat",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateRiskAssessmentAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "RiskAssessment",
                "Risk Assessment",
                procurement,
                ct
            );
        }

        public async Task<byte[]> GenerateOwnerEstimateAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("OwnerEstimate", "Owner Estimate", procurement, ct);
        }

        public async Task<byte[]> GenerateBOQAsync(
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("BOQ", "Bill of Quantity", procurement, ct);
        }

        public async Task<byte[]> GenerateFromTemplateAsync(
            string templateName,
            object model,
            CancellationToken ct = default
        )
        {
            _logger.LogInformation("Generating from custom template: {Template}", templateName);

            var template = await _templateProvider.GetTemplateAsync(templateName, ct);
            string html;

            if (model is Procurement ProcurementModel)
            {
                html = await _tokenReplacer.ReplaceTokensAsync(template, ProcurementModel, ct);
            }
            else
            {
                html = template;
                var type = model.GetType();

                foreach (var prop in type.GetProperties())
                {
                    var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                    html = html.Replace($"{{{{{prop.Name}}}}}", value);
                }
            }

            return await HtmlToPdfAsync(html, templateName, ct);
        }

        #region Utilities

        private async Task<byte[]> GenerateByTemplateAsync(
            string templateKey,
            string title,
            Procurement procurement,
            CancellationToken ct
        )
        {
            var template = await _templateProvider.GetTemplateAsync(templateKey, ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, procurement, ct, templateKey);

            return await HtmlToPdfAsync(html, title, ct);
        }

        private static async Task<byte[]> HtmlToPdfAsync(
            string html,
            string title,
            CancellationToken ct
        )
        {
            using var pw = await Playwright.CreateAsync();
            await using var browser = await pw.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true }
            );
            var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });

            var pdf = await page.PdfAsync(
                new()
                {
                    Format = "A4",
                    PrintBackground = true,
                    DisplayHeaderFooter = false,
                    Margin = new()
                    {
                        Top = "12mm",
                        Right = "12mm",
                        Bottom = "14mm",
                        Left = "12mm",
                    },
                    HeaderTemplate =
                        $"<div style=\"font-size: 10px; width: 100%; text-align: right; padding-right: 8px;\">{System.Net.WebUtility.HtmlEncode(title)}</div>",
                    FooterTemplate =
                        "<div style='font-size: 10px; width: 100%; text-align: right; padding-right: 8px;'>Hal <span class=\"pageNumber\"></span><span class=\"totalPages\"></span></div>",
                }
            );

            return pdf;
        }

        #endregion
    }
}
