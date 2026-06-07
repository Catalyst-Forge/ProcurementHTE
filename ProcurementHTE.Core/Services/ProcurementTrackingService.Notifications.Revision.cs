using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private async Task SendRevisionNotificationToPicOps(
        Procurement procurement,
        RejectionSymptom symptoms,
        string rejectionNote,
        CancellationToken ct
    )
    {
        var picOpsUserId = procurement.PicOpsUserId;
        if (string.IsNullOrEmpty(picOpsUserId))
        {
            _logger.LogWarning(
                "Cannot send revision notification: PicOpsUserId is empty for procurement {ProcurementId}",
                procurement.ProcurementId
            );
            return;
        }

        var symptomNames = string.Join(", ", symptoms.GetDataIssues().GetSelectedDisplayNames());
        await _notificationService.SendNotificationAsync(
            userId: picOpsUserId,
            title: $"Procurement {procurement.ProcNum} perlu revisi data",
            message: $"WO: {procurement.Wonum}\nIssue: {symptomNames}\nCatatan: {rejectionNote}",
            notificationType: "NeedsRevisionData",
            actionUrl: $"/Procurements/Edit/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: procurement.RejectedByUserId,
            ct: ct
        );
    }

    private async Task SendRevisionNotificationToAppo(
        Procurement procurement,
        RejectionSymptom symptoms,
        string rejectionNote,
        CancellationToken ct
    )
    {
        var appoUserId = procurement.AppoUserId;
        if (string.IsNullOrEmpty(appoUserId))
        {
            _logger.LogWarning(
                "Cannot send revision notification: AppoUserId is empty for procurement {ProcurementId}",
                procurement.ProcurementId
            );
            return;
        }

        var symptomNames = string.Join(", ", symptoms.GetPRIssues().GetSelectedDisplayNames());
        var message = symptoms.HasPRCannotBeCombined()
            ? $"WO: {procurement.Wonum}\nâš ï¸ PERLU DILEPAS DARI PR\nIssue: {symptomNames}\nCatatan: {rejectionNote}"
            : $"WO: {procurement.Wonum}\nIssue: {symptomNames}\nCatatan: {rejectionNote}";

        await _notificationService.SendNotificationAsync(
            userId: appoUserId,
            title: $"Procurement {procurement.ProcNum} perlu revisi PR/Dokumen",
            message: message,
            notificationType: "NeedsRevisionPR",
            actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: procurement.RejectedByUserId,
            ct: ct
        );
    }

    private async Task SendUnlinkNotificationToAppo(
        Procurement procurement,
        string appoUserId,
        string? oldPrId,
        CancellationToken ct
    )
    {
        await _notificationService.SendNotificationAsync(
            userId: appoUserId,
            title: $"Procurement {procurement.ProcNum} dilepas dari PR",
            message: $"WO: {procurement.Wonum}\nProcurement telah dilepas dari PR {oldPrId} karena tidak bisa digabung.\nProcurement ini perlu di-pickup ulang.",
            notificationType: "ProcurementUnlinked",
            actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: procurement.ResubmittedByUserId,
            ct: ct
        );
    }

    private async Task SendProcurementNeedsPickupNotification(
        Procurement procurement,
        CancellationToken ct
    )
    {
        await _notificationService.SendNotificationToRoleAsync(
            roleName: "AP-PO",
            title: "Procurement perlu di-pickup",
            message: $"WO: {procurement.Wonum}\nProcurement {procurement.ProcNum} tersedia untuk di-pickup.\n(Revisi ke-{procurement.RevisionCount})",
            notificationType: "ProcurementNeedsPickup",
            actionUrl: $"/Procurements/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: null,
            ct: ct
        );
    }
}
