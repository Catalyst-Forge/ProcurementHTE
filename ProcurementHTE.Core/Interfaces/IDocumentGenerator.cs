using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentGenerator
    {
        Task<byte[]> GenerateMemorandumAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GeneratePermintaanPekerjaanAsync(
            Procurement procurement,
            CancellationToken ct = default
        );
        Task<byte[]> GenerateServiceOrderAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GenerateMarketSurveyAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GenerateSPMPAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GenerateSuratPenawaranHargaAsync(
            Procurement procurement,
            CancellationToken ct = default
        );
        Task<byte[]> GenerateSuratNegosiasiHargaAsync(
            Procurement procurement,
            CancellationToken ct = default
        );
        Task<byte[]> GenerateRKSAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GenerateRiskAssessmentAsync(
            Procurement procurement,
            CancellationToken ct = default
        );
        Task<byte[]> GenerateOwnerEstimateAsync(
            Procurement procurement,
            CancellationToken ct = default
        );
        Task<byte[]> GenerateBOQAsync(Procurement procurement, CancellationToken ct = default);
        Task<byte[]> GenerateProfitLossAsync(Procurement procurement, CancellationToken ct = default);

        // Generic method untuk custom templates
        Task<byte[]> GenerateFromTemplateAsync(
            string templateName,
            object model,
            CancellationToken ct = default
        );
    }
}
