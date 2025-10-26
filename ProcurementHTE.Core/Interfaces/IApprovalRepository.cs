using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Models;
using System;

namespace ProcurementHTE.Core.Interfaces {
    public interface IApprovalRepository {
        Task<IReadOnlyList<WoDocumentApprovals>> GetPendingApprovalsForUserAsync(User user);

        Task<(bool AllDocsApproved, string WorkOrderId)> ApproveAsync(string approvalId, string approverUserId);

        Task RejectAsync(string approvalId, string approverUserId, string? note);
    }
}
