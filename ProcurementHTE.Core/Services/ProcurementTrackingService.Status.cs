using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<bool> UpdateProcurementStatusAsync(
        string procurementId,
        ProcurementStatus newStatus,
        string? changedByUserId = null,
        string? note = null,
        CancellationToken ct = default
    )
    {
        var success = await _procurementRepo.UpdateStatusWithHistoryAsync(
            procurementId,
            newStatus,
            changedByUserId,
            note,
            ct
        );

        if (!success)
            return false;

        _logger.LogInformation(
            "Procurement {ProcurementId} status changed to {NewStatus} by user {UserId}",
            procurementId,
            newStatus,
            changedByUserId ?? "System"
        );

        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement?.PrId != null)
            await RecalculatePrStatusAsync(procurement.PrId, ct);

        return true;
    }

    public async Task RecalculatePrStatusAsync(string prId, CancellationToken ct = default)
    {
        var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
        if (pr == null)
            return;

        var newDerivedStatus = pr.DerivedStatus;
        if (pr.Status == newDerivedStatus)
            return;

        var oldStatus = pr.Status;
        pr.Status = newDerivedStatus;
        pr.UpdatedAt = DateTime.UtcNow;

        await _prRepo.UpdateAsync(pr, ct);
        await _prRepo.AddStatusHistoryAsync(
            prId,
            newDerivedStatus,
            null,
            "Recalculated from procurement statuses",
            ct
        );
        await _prRepo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PR {PrId} status recalculated from {OldStatus} to {NewStatus}",
            prId,
            oldStatus,
            newDerivedStatus
        );
    }
}
