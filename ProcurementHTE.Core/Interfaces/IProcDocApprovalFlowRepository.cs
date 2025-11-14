using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IProcDocApprovalFlowRepository {
        Task<ProcDocuments?> GetDocumentWithProcurementAsync(string procDocumentId, CancellationToken ct = default);
        Task<JobTypeDocuments?> GetJobTypeDocumentWithApprovalsAsync(string jobTypeId, string documentTypeId, CancellationToken ct = default);
        Task AddApprovalsAsync(IEnumerable<ProcDocumentApprovals> approvals, CancellationToken ct = default);
        Task UpdateProcDocumentStatusAsync(string procDocumentId, string newStatus, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
