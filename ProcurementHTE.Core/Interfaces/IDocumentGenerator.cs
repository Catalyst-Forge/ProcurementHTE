using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IDocumentGenerator {
        Task<byte[]> GenerateMemorandumAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GeneratePermintaanPekerjaanAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateServiceOrderAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateMarketSurveyAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateSPMPAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateSuratPenawaranHargaAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateSuratNegosiasiHargaAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateRKSAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateRiskAssessmentAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateOwnerEstimateAsync(WorkOrder workOrder, CancellationToken ct = default);
        Task<byte[]> GenerateBOQAsync(WorkOrder workOrder, CancellationToken ct = default);

        // Generic method untuk custom templates
        Task<byte[]> GenerateFromTemplateAsync(string templateName, object model, CancellationToken ct = default);
    }
}
