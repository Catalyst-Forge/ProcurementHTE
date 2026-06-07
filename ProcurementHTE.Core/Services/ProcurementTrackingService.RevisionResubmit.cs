using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> ResubmitRevisionAsync(
        string procurementId,
        string submittedByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        var currentStatus = procurement.ProcurementStatus;
        var pendingSymptoms = procurement.PendingRevisionSymptoms ?? RejectionSymptom.None;

        if (
            currentStatus != ProcurementStatus.NeedsRevisionData
            && currentStatus != ProcurementStatus.NeedsRevisionPR
        )
            return Failed("Procurement tidak dalam status revisi.");

        if (currentStatus == ProcurementStatus.NeedsRevisionData)
            return await ResubmitDataRevisionAsync(procurement, pendingSymptoms, submittedByUserId, ct);

        return await ResubmitPrRevisionAsync(procurement, pendingSymptoms, submittedByUserId, ct);
    }

    private async Task<ProcurementTrackingResponse> ResubmitDataRevisionAsync(
        Procurement procurement,
        RejectionSymptom pendingSymptoms,
        string submittedByUserId,
        CancellationToken ct
    )
    {
        pendingSymptoms = pendingSymptoms.GetPRIssues();
        procurement.PendingRevisionSymptoms = pendingSymptoms;

        ProcurementStatus newStatus;
        string message;
        if (pendingSymptoms.HasPRIssues())
        {
            newStatus = ProcurementStatus.NeedsRevisionPR;
            message = "Revisi data selesai. Lanjut ke revisi PR/Dokumen oleh APPO.";
            await SendRevisionNotificationToAppo(
                procurement,
                pendingSymptoms,
                procurement.RejectionNote ?? "Lanjutan revisi dari data issue",
                ct
            );
        }
        else
        {
            newStatus = ProcurementStatus.WaitingApprovalAnalyst;
            message = "Revisi selesai. Procurement dikirim ulang ke approval Analyst HTE.";
            CompleteRevision(procurement, submittedByUserId);
        }

        return await CompleteResubmissionAsync(procurement, newStatus, message, submittedByUserId, ct);
    }

    private async Task<ProcurementTrackingResponse> ResubmitPrRevisionAsync(
        Procurement procurement,
        RejectionSymptom pendingSymptoms,
        string submittedByUserId,
        CancellationToken ct
    )
    {
        if (pendingSymptoms.HasPRCannotBeCombined())
            return await UnlinkAndResetProcurementAsync(procurement.ProcurementId, submittedByUserId, ct);

        procurement.PendingRevisionSymptoms = RejectionSymptom.None;
        CompleteRevision(procurement, submittedByUserId);
        return await CompleteResubmissionAsync(
            procurement,
            ProcurementStatus.OnCreateDP3,
            "Revisi PR/Dokumen selesai. APPO dapat melanjutkan proses.",
            submittedByUserId,
            ct
        );
    }

    private async Task<ProcurementTrackingResponse> CompleteResubmissionAsync(
        Procurement procurement,
        ProcurementStatus newStatus,
        string message,
        string submittedByUserId,
        CancellationToken ct
    )
    {
        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurement.ProcurementId,
            newStatus,
            submittedByUserId,
            $"Resubmitted after revision. Count: {procurement.RevisionCount}",
            ct
        );

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = message,
            Data = await GetTrackingByProcurementIdAsync(procurement.ProcurementId, ct),
        };
    }

    private static void CompleteRevision(Procurement procurement, string submittedByUserId)
    {
        procurement.ResubmittedAt = DateTime.UtcNow;
        procurement.ResubmittedByUserId = submittedByUserId;
        procurement.RevisionCount++;
        procurement.RejectionNote = null;
        procurement.RejectedAt = null;
        procurement.RejectedByUserId = null;
        procurement.RejectionSymptoms = null;
        procurement.PendingRevisionSymptoms = null;
        procurement.StatusBeforeRejection = null;
    }
}
