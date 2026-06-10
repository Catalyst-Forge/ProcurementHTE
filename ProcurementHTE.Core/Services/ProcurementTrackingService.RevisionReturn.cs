using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> RejectProcurementAsync(
        string procurementId,
        string rejectionNote,
        string rejectedByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        procurement.RejectionNote = rejectionNote;
        procurement.RejectedAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.RejectedByUserId = rejectedByUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.Rejected,
            rejectedByUserId,
            $"Rejected: {rejectionNote}",
            ct
        );

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "Procurement telah ditolak.",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    public async Task<ProcurementTrackingResponse> ReturnForRevisionAsync(
        string procurementId,
        RejectionSymptom symptoms,
        string rejectionNote,
        string rejectedByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (symptoms == RejectionSymptom.None)
            return Failed("Minimal satu symptom harus dipilih.");

        procurement.StatusBeforeRejection = procurement.ProcurementStatus;
        procurement.RejectionSymptoms = symptoms;
        procurement.PendingRevisionSymptoms = symptoms;
        procurement.RejectionNote = rejectionNote;
        procurement.RejectedAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.RejectedByUserId = rejectedByUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var (newStatus, notificationTarget) = ResolveRevisionTarget(symptoms);
        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            newStatus,
            rejectedByUserId,
            $"Returned for revision: {rejectionNote}. Symptoms: {string.Join(", ", symptoms.GetSelectedDisplayNames())}",
            ct
        );

        if (notificationTarget == "PIC_OPS")
            await SendRevisionNotificationToPicOps(procurement, symptoms, rejectionNote, ct);
        else
            await SendRevisionNotificationToAppo(procurement, symptoms, rejectionNote, ct);

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = $"Procurement dikembalikan untuk revisi ({GetProcurementStatusDescription(newStatus)}).",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    private static (ProcurementStatus Status, string Target) ResolveRevisionTarget(
        RejectionSymptom symptoms
    )
    {
        if (symptoms.HasDataIssues())
            return (ProcurementStatus.NeedsRevisionData, "PIC_OPS");

        if (symptoms.HasPRIssues())
            return (ProcurementStatus.NeedsRevisionPR, "APPO");

        return (ProcurementStatus.NeedsRevisionData, "PIC_OPS");
    }
}
