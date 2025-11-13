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
        private readonly IWorkOrderRepository _woRepository;
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IVendorRepository _vendorRepository;

        public DocumentGenerator(
            ITemplateProvider templateProvider,
            IHtmlTokenReplacer tokenReplacer,
            ILogger<DocumentGenerator> logger,
            IWorkOrderRepository woRepository,
            IProfitLossRepository pnlRepository,
            IVendorRepository vendorRepository
        )
        {
            _templateProvider = templateProvider;
            _tokenReplacer = tokenReplacer;
            _logger = logger;
            _woRepository = woRepository;
            _pnlRepository = pnlRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<byte[]> GenerateProfitLossAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            _logger.LogInformation(
                "Generating Profit & Loss (Playwright) for WO: {WoNum}",
                workOrder.WoNum
            );

            var template = await _templateProvider.GetTemplateAsync("ProfitLoss", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            var pdf = await HtmlToPdfAsync(html, "Profit & Loss", ct);

            _logger.LogInformation("PDF P&L generated, size={Size} bytes", pdf.Length);

            return pdf;
        }

        public async Task<byte[]> GenerateMemorandumAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("Memorandum", "Memorandum", workOrder, ct);
        }

        public async Task<byte[]> GeneratePermintaanPekerjaanAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "PermintaanPekerjaan",
                "Permintaan Pekerjaan",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateServiceOrderAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("ServiceOrder", "Service Order", workOrder, ct);
        }

        public async Task<byte[]> GenerateMarketSurveyAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            var template = await _templateProvider.GetTemplateAsync("MarketSurvey", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            var pnl = await _pnlRepository.GetLatestByWorkOrderIdAsync(workOrder.WorkOrderId);
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
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SPMP",
                "Surat Perintah Mulai Pekerjaan",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateSuratPenawaranHargaAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SuratPenawaranHarga",
                "Surat Penawaran Harga",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateSuratNegosiasiHargaAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "SuratNegosiasiHarga",
                "Surat Negosiasi Harga",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateRKSAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "RKS",
                "Rencana Kerja dan Syarat-Syarat",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateRiskAssessmentAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync(
                "RiskAssessment",
                "Risk Assessment",
                workOrder,
                ct
            );
        }

        public async Task<byte[]> GenerateOwnerEstimateAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("OwnerEstimate", "Owner Estimate", workOrder, ct);
        }

        public async Task<byte[]> GenerateBOQAsync(
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            return await GenerateByTemplateAsync("BOQ", "Bill of Quantity", workOrder, ct);
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

            if (model is WorkOrder woModel)
            {
                html = await _tokenReplacer.ReplaceTokensAsync(template, woModel, ct);
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
            WorkOrder workOrder,
            CancellationToken ct
        )
        {
            var template = await _templateProvider.GetTemplateAsync(templateKey, ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);

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
                    DisplayHeaderFooter = true,
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
