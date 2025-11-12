using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IApprovalRepository
    {
        // ===== EXISTING (contoh, biarkan yang lama tetap ada) =====
        Task<(bool AllDocsApproved, string WorkOrderId)> ApproveAsync(
            string approvalId,
            string approverUserId
        );
        Task RejectAsync(string approvalId, string approverUserId, string? note);
        Task<IReadOnlyList<WoDocumentApprovals>> GetPendingApprovalsForUserAsync(User user);

        Task<GateInfoDto?> GetCurrentPendingGateByQrAsync(
            string qrText,
            CancellationToken ct = default
        );
        Task<GateInfoDto?> GetCurrentPendingGateByApprovalIdAsync(
            string woDocumentApprovalId,
            CancellationToken ct = default
        );

        // ===== NEW / DIPAKAI SERVICE =====
        Task<IReadOnlyList<string>> GetUserRoleNamesAsync(
            string userId,
            CancellationToken ct = default
        );
        Task<IReadOnlyList<string>> GetUserRoleIdsAsync(
            string userId,
            CancellationToken ct = default
        );

        Task<WoDocumentApprovals?> GetLastApprovalByUserOnDocumentAsync(
            string userId,
            string woDocumentId,
            CancellationToken ct = default
        );

        Task<IReadOnlyList<string>> GetExistingRoleIdsAsync(
            IEnumerable<string> roleIds,
            CancellationToken ct = default
        );
        Task<IReadOnlyList<string>> GetExistingRoleNamesAsync(
            IEnumerable<string> roleNames,
            CancellationToken ct = default
        );
        Task<int> CountUsersWithAnyRoleAsync(
            IEnumerable<string> roleIds,
            IEnumerable<string> roleNames,
            CancellationToken ct = default
        );

        Task<IReadOnlyList<ApprovalStepDto>> GetDocumentApprovalChainAsync(
            string woDocumentId,
            CancellationToken ct = default
        );

        Task<GateInfoDto?> GetCurrentPendingGateByDocumentIdAsync(
            string woDocumentId,
            CancellationToken ct = default
        );
        Task<RejectionInfoDto?> GetLastRejectionInfoAsync(
            string woDocumentId,
            CancellationToken ct = default
        );
    }
}
