using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public sealed class ProcurementDocumentGenerator : IProcurementDocumentGenerator
{
    private static readonly IReadOnlyDictionary<
        string,
        Func<IDocumentGenerator, Procurement, CancellationToken, Task<byte[]>>
    > Generators = new Dictionary<
        string,
        Func<IDocumentGenerator, Procurement, CancellationToken, Task<byte[]>>
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["Memorandum"] = (generator, procurement, ct) =>
            generator.GenerateMemorandumAsync(procurement, ct),
        ["Permintaan Pekerjaan"] = (generator, procurement, ct) =>
            generator.GeneratePermintaanPekerjaanAsync(procurement, ct),
        ["Service Order"] = (generator, procurement, ct) =>
            generator.GenerateServiceOrderAsync(procurement, ct),
        ["Market Survey"] = (generator, procurement, ct) =>
            generator.GenerateMarketSurveyAsync(procurement, ct),
        ["Surat Perintah Mulai Pekerjaan (SPMP)"] = (generator, procurement, ct) =>
            generator.GenerateSPMPAsync(procurement, ct),
        ["Surat Penawaran Harga"] = (generator, procurement, ct) =>
            generator.GenerateSuratPenawaranHargaAsync(procurement, ct),
        ["Surat Negosiasi Harga"] = (generator, procurement, ct) =>
            generator.GenerateSuratNegosiasiHargaAsync(procurement, ct),
        ["Rencana Kerja dan Syarat-Syarat (RKS)"] = (generator, procurement, ct) =>
            generator.GenerateRKSAsync(procurement, ct),
        ["Risk Assessment (RA)"] = (generator, procurement, ct) =>
            generator.GenerateRiskAssessmentAsync(procurement, ct),
        ["Owner Estimate (OE)"] = (generator, procurement, ct) =>
            generator.GenerateOwnerEstimateAsync(procurement, ct),
        ["Bill of Quantity (BOQ)"] = (generator, procurement, ct) =>
            generator.GenerateBOQAsync(procurement, ct),
        ["Profit & Loss"] = (generator, procurement, ct) =>
            generator.GenerateProfitLossAsync(procurement, ct),
        ["Justifikasi"] = (generator, procurement, ct) =>
            generator.GenerateJustifikasiAsync(procurement, ct),
    };

    private readonly IDocumentGenerator _documentGenerator;

    public ProcurementDocumentGenerator(IDocumentGenerator documentGenerator)
    {
        _documentGenerator =
            documentGenerator ?? throw new ArgumentNullException(nameof(documentGenerator));
    }

    public async Task<ProcurementDocumentGenerationResult> GenerateAsync(
        string? documentTypeName,
        Procurement procurement,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(documentTypeName))
            return ProcurementDocumentGenerationResult.Unsupported(documentTypeName);

        if (!Generators.TryGetValue(documentTypeName.Trim(), out var generate))
            return ProcurementDocumentGenerationResult.Unsupported(documentTypeName);

        var bytes = await generate(_documentGenerator, procurement, ct);
        return ProcurementDocumentGenerationResult.SuccessResult(bytes);
    }
}
