using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> UnlinkAndResetProcurementAsync(
        string procurementId,
        string resetByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        var oldPrId = procurement.PrId;
        var oldAppoUserId = procurement.AppoUserId;

        procurement.PrId = null;
        procurement.AppoUserId = null;
        procurement.PickedUpAt = null;
        procurement.PendingRevisionSymptoms = RejectionSymptom.None;
        procurement.ResubmittedAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.ResubmittedByUserId = resetByUserId;
        procurement.RevisionCount++;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.OnCreateDP3,
            resetByUserId,
            $"Unlinked from PR due to PRCannotBeCombined. Previous PR: {oldPrId}",
            ct
        );

        if (!string.IsNullOrEmpty(oldPrId))
            await UpdateOldPrAfterUnlinkAsync(oldPrId, resetByUserId, ct);

        if (!string.IsNullOrEmpty(oldAppoUserId))
            await SendUnlinkNotificationToAppo(procurement, oldAppoUserId, oldPrId, ct);

        await SendProcurementNeedsPickupNotification(procurement, ct);

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "Procurement telah dilepas dari PR dan perlu di-pickup ulang oleh APPO.",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    private async Task UpdateOldPrAfterUnlinkAsync(
        string oldPrId,
        string resetByUserId,
        CancellationToken ct
    )
    {
        var oldPr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(oldPrId, ct);
        if (oldPr == null)
            return;

        var remainingProcs = await _procurementRepo.GetByPrIdWithTrackingAsync(oldPrId, ct);
        if (remainingProcs.Any())
        {
            await RecalculatePrStatusAsync(oldPrId, ct);
            return;
        }

        oldPr.Status = PurchaseRequisitionStatus.ReturnedFromProcurement;
        oldPr.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _prRepo.UpdateAsync(oldPr, ct);
        await _prRepo.AddStatusHistoryAsync(
            oldPrId,
            PurchaseRequisitionStatus.ReturnedFromProcurement,
            resetByUserId,
            "All procurements unlinked due to PRCannotBeCombined",
            ct
        );
        await _prRepo.SaveChangesAsync(ct);
    }
}
