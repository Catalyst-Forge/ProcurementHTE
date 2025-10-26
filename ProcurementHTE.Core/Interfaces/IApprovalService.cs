using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IApprovalService {
        Task<IReadOnlyList<WoDocumentApprovals>> GetPendingApprovalsForUserAsync(User user);
        Task ApproveAsync(string approvalId, string approverUserId);
        Task RejectAsync(string approvalId, string approverUserId, string? note);
    }
}
