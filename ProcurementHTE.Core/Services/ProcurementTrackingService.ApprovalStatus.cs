using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<bool> HandleApprovalStatusChangeAsync(
        string procurementId,
        string approvalAction,
        string? approverUserId = null,
        string? note = null,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return false;

        var currentStatus = procurement.ProcurementStatus;
        if (approvalAction.Equals("approve", StringComparison.OrdinalIgnoreCase))
            return await HandleApprovalAsync(procurement, currentStatus, approverUserId, note, ct);

        if (approvalAction.Equals("reject", StringComparison.OrdinalIgnoreCase))
            return await HandleApprovalRejectionAsync(procurement, currentStatus, approverUserId, note, ct);

        return false;
    }

    private async Task<bool> HandleApprovalAsync(
        Procurement procurement,
        ProcurementStatus currentStatus,
        string? approverUserId,
        string? note,
        CancellationToken ct
    )
    {
        var ctValue = await GetCtAsync(procurement.ProcurementId);
        var requiredLevel = ApprovalConstants.GetRequiredApprovalLevel(ctValue);
        var newStatus = GetNextApprovalStatus(currentStatus, requiredLevel);
        if (newStatus == currentStatus)
            return false;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        SetApprovalTimelineOnStatusChange(procurement, currentStatus, newStatus, now);
        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurement.ProcurementId,
            newStatus,
            approverUserId,
            note ?? "Approved",
            ct
        );

        await SendApprovalProgressNotification(
            procurement,
            currentStatus,
            newStatus,
            approverUserId,
            ct
        );

        if (newStatus == ProcurementStatus.OnSubmitISPA)
            await SendReadyForIspaNotification(procurement, ct);
        else
            await SendApprovalNotification(procurement, ct);

        return true;
    }

    private async Task<bool> HandleApprovalRejectionAsync(
        Procurement procurement,
        ProcurementStatus currentStatus,
        string? approverUserId,
        string? note,
        CancellationToken ct
    )
    {
        procurement.RejectionNote = note ?? "Rejected by approver";
        procurement.RejectedAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.RejectedByUserId = approverUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurement.ProcurementId,
            ProcurementStatus.Rejected,
            approverUserId,
            note ?? "Rejected",
            ct
        );
        await SendRejectionNotification(procurement, currentStatus, approverUserId, note, ct);

        return true;
    }
}
