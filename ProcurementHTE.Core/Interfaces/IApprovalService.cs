using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IApprovalService
    {
        Task<IReadOnlyList<ProcDocumentApprovals>> GetPendingApprovalsForUserAsync(User user);
        Task ApproveAsync(string approvalId, string approverUserId);
        Task RejectAsync(string approvalId, string approverUserId, string? note);
        Task<GateInfoDto?> GetCurrentPendingGateByQrAsync(
            string qrText,
            CancellationToken ct = default
        );
        Task<GateInfoDto?> GetCurrentPendingGateByApprovalIdAsync(
            string procDocumentApprovalId,
            CancellationToken ct = default
        );
        Task<ApprovalUpdateResult> UpdateStatusByQrAsync(
            string qrText,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        );
        Task<ApprovalUpdateResult> UpdateStatusByApprovalIdAsync(
            string approvalId,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        );
        Task<ApprovalUpdateResult> UpdateStatusByDocumentIdAsync(
            string procDocumentId,
            string action,
            string? note,
            User currentUser,
            CancellationToken ct = default
        );

        // IApprovalService.cs
        Task<ApprovalTimelineDto?> GetApprovalTimelineAsync(
            string procDocumentId,
            CancellationToken ct = default
        );
    }
}
