using System;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Services;

public partial class DocumentGenerator
{
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

        html = ApplyCommonTokens(html);

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
        return await GenerateByTemplateAsync("RiskAssessment", "Risk Assessment", procurement, ct);
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

    public async Task<byte[]> GenerateJustifikasiAsync(
        Procurement procurement,
        CancellationToken ct = default
    )
    {
        return await GenerateByTemplateAsync("Justifikasi", "Justifikasi", procurement, ct);
    }
}
