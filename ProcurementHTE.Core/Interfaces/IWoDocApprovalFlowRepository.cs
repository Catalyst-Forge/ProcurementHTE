using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IWoDocApprovalFlowRepository {
        Task<WoDocuments?> GetDocumentWithWorkOrderAsync(string woDocumentId, CancellationToken ct = default);
        Task<WoTypeDocuments?> GetWoTypeDocumentWithApprovalsAsync(string woTypeId, string documentTypeId, CancellationToken ct = default);
        Task AddApprovalsAsync(IEnumerable<WoDocumentApprovals> approvals, CancellationToken ct = default);
        Task UpdateWoDocumentStatusAsync(string woDocumentId, string newStatus, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
