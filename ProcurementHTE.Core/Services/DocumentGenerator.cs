using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace ProcurementHTE.Core.Services {
    public class DocumentGenerator : IDocumentGenerator {
        private readonly IConverter _converter;
        private readonly ITemplateProvider _templateProvider;
        private readonly IHtmlTokenReplacer _tokenReplacer;
        private readonly ILogger<DocumentGenerator> _logger;
        private readonly IWorkOrderRepository _woRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;

        public DocumentGenerator(
            IConverter converter,
            ITemplateProvider templateProvider,
            IHtmlTokenReplacer tokenReplacer,
            ILogger<DocumentGenerator> logger,
            IWorkOrderRepository woRepo,
            IProfitLossRepository pnlRepo,
            IVendorRepository vendorRepo) {
            _converter = converter;
            _templateProvider = templateProvider;
            _tokenReplacer = tokenReplacer;
            _logger = logger;
            _woRepo = woRepo;
            _pnlRepo = pnlRepo;
            _vendorRepo = vendorRepo;
        }

        public async Task<byte[]> GenerateMemorandumAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Memorandum for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("Memorandum", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Memorandum");
        }

        public async Task<byte[]> GeneratePermintaanPekerjaanAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Permintaan Pekerjaan for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("PermintaanPekerjaan", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Permintaan Pekerjaan");
        }

        public async Task<byte[]> GenerateServiceOrderAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Service Order for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("ServiceOrder", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Service Order");
        }

        public async Task<byte[]> GenerateMarketSurveyAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Market Survey for WO: {WoNum}", workOrder.WoNum);

            // Ambil data vendor & penawaran untuk Market Survey
            var vendors = await _vendorRepo.GetAllAsync();
            var pnl = await _pnlRepo.GetLatestByWorkOrderIdAsync(workOrder.WorkOrderId);

            var template = await _templateProvider.GetTemplateAsync("MarketSurvey", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);

            // Replace vendor-specific tokens (jika belum ada di token replacer)
            if (pnl != null) {
                var selectedVendor = vendors.FirstOrDefault(v => v.VendorId == pnl.SelectedVendorId);
                html = html.Replace("{{SelectedVendorName}}", selectedVendor?.VendorName ?? "-");
                html = html.Replace("{{SelectedVendorPrice}}", pnl.SelectedVendorFinalOffer.ToString("N0"));
            }

            return ConvertHtmlToPdf(html, "Market Survey");
        }

        public async Task<byte[]> GenerateSPMPAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating SPMP for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("SPMP", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "SPMP");
        }

        public async Task<byte[]> GenerateSuratPenawaranHargaAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Surat Penawaran Harga for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("SuratPenawaranHarga", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Surat Penawaran Harga");
        }

        public async Task<byte[]> GenerateSuratNegosiasiHargaAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Surat Negosiasi Harga for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("SuratNegosiasiHarga", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Surat Negosiasi Harga");
        }

        public async Task<byte[]> GenerateRKSAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating RKS for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("RKS", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "RKS");
        }

        public async Task<byte[]> GenerateRiskAssessmentAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Risk Assessment for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("RiskAssessment", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Risk Assessment");
        }

        public async Task<byte[]> GenerateOwnerEstimateAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating Owner Estimate for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("OwnerEstimate", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "Owner Estimate");
        }

        public async Task<byte[]> GenerateBOQAsync(WorkOrder workOrder, CancellationToken ct = default) {
            _logger.LogInformation("Generating BOQ for WO: {WoNum}", workOrder.WoNum);
            var template = await _templateProvider.GetTemplateAsync("BOQ", ct);
            var html = await _tokenReplacer.ReplaceTokensAsync(template, workOrder, ct);
            return ConvertHtmlToPdf(html, "BOQ");
        }

        public async Task<byte[]> GenerateFromTemplateAsync(string templateName, object model, CancellationToken ct = default) {
            _logger.LogInformation("Generating from custom template: {TemplateName}", templateName);
            var template = await _templateProvider.GetTemplateAsync(templateName, ct);

            // Simple token replacement for generic models
            var html = template;
            var properties = model.GetType().GetProperties();
            foreach (var prop in properties) {
                var value = prop.GetValue(model)?.ToString() ?? "";
                html = html.Replace($"{{{{{prop.Name}}}}}", value);
            }

            return ConvertHtmlToPdf(html, templateName);
        }

        private byte[] ConvertHtmlToPdf(string html, string documentTitle) {
            // Optional: Save HTML untuk debugging
            if (_logger.IsEnabled(LogLevel.Debug)) {
                var debugPath = Path.Combine(Path.GetTempPath(), $"{documentTitle}_{DateTime.Now:yyyyMMddHHmmss}.html");
                File.WriteAllText(debugPath, html);
                _logger.LogDebug("HTML saved to: {Path}", debugPath);
            }

            var doc = new HtmlToPdfDocument {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                DocumentTitle = documentTitle,
            },
                Objects = {
                new ObjectSettings {
                    PagesCount = true,
                    HtmlContent = html,
                    WebSettings = {
                        DefaultEncoding = "utf-8",
                        EnableJavascript = false,
                        LoadImages = true,
                        PrintMediaType = true
                    },
                    HeaderSettings = {
                        FontSize = 9,
                        Right = "Halaman [page] dari [toPage]",
                        Line = true,
                        Spacing = 2.812
                    },
                    FooterSettings = {
                        FontSize = 9,
                        Center = $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}",
                        Line = true,
                        Spacing = 2.812
                    }
                }
            }
            };

            try {
                var pdfBytes = _converter.Convert(doc);
                _logger.LogInformation("PDF generated successfully: {Title}, Size: {Size} bytes", documentTitle, pdfBytes.Length);
                return pdfBytes;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error converting HTML to PDF for document: {Title}", documentTitle);
                throw new InvalidOperationException($"Gagal generate PDF '{documentTitle}': {ex.Message}", ex);
            }
        }
    }
}
