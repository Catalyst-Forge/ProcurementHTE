using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private async Task SendApprovalNotification(Procurement procurement, CancellationToken ct)
    {
        var approverUserId = procurement.ProcurementStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => procurement.AnalystHteUserId,
            ProcurementStatus.WaitingApprovalAsstManager => procurement.AssistantManagerUserId,
            ProcurementStatus.WaitingApprovalManager => procurement.ManagerUserId,
            ProcurementStatus.WaitingApprovalVP => procurement.VicePresidentUserId,
            ProcurementStatus.WaitingApprovalOpDir => procurement.OperationDirectorUserId,
            ProcurementStatus.WaitingApprovalPresDir => procurement.PresidentDirectorUserId,
            _ => null,
        };

        if (string.IsNullOrEmpty(approverUserId))
            return;

        await _notificationService.SendNotificationAsync(
            userId: approverUserId,
            title: $"{procurement.DisplayName} menunggu approval Anda",
            message: $"WO: {procurement.Wonum} - {procurement.JobName}",
            notificationType: "ApprovalRequest",
            actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: procurement.ApprovalSentByUserId,
            ct: ct
        );

        await _notificationPusher.PushApprovalBadgeAsync(approverUserId, -1);
    }

    private async Task SendApprovalProgressNotification(
        Procurement procurement,
        ProcurementStatus previousStatus,
        ProcurementStatus newStatus,
        string? approverUserId,
        CancellationToken ct
    )
    {
        var submitterUserId = procurement.ApprovalSentByUserId;
        if (string.IsNullOrEmpty(submitterUserId) || submitterUserId == approverUserId)
            return;

        var approverRole = previousStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
            ProcurementStatus.WaitingApprovalAsstManager => RoleNames.AsstManager,
            ProcurementStatus.WaitingApprovalManager => "Manager Transport & Logistic",
            ProcurementStatus.WaitingApprovalVP => "Vice President",
            ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
            ProcurementStatus.WaitingApprovalPresDir => "President Director",
            _ => "Approver",
        };
        var nextStep = newStatus switch
        {
            ProcurementStatus.WaitingApprovalAsstManager => "Menunggu approval Asst. Manager",
            ProcurementStatus.WaitingApprovalManager => "Menunggu approval Manager",
            ProcurementStatus.WaitingApprovalVP => "Menunggu approval Vice President",
            ProcurementStatus.WaitingApprovalOpDir => "Menunggu approval Operation Director",
            ProcurementStatus.WaitingApprovalPresDir => "Menunggu approval President Director",
            ProcurementStatus.OnSubmitISPA => "Semua approval selesai, siap submit INPRO",
            _ => "Proses berlanjut",
        };

        await _notificationService.SendNotificationAsync(
            userId: submitterUserId,
            title: $"{procurement.DisplayName} telah di-approve oleh {approverRole}",
            message: $"WO: {procurement.Wonum} - {nextStep}",
            notificationType: $"ApprovedBy{approverRole.Replace(" ", "")}",
            actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: approverUserId,
            ct: ct
        );
    }

    private async Task SendReadyForIspaNotification(Procurement procurement, CancellationToken ct)
    {
        await NotifyApPoUsersAsync(
            title: $"{procurement.DisplayName} sudah selesai approval",
            message: $"WO: {procurement.Wonum} - Semua approval telah selesai",
            notificationType: "ReadyForISPA",
            actionUrl: "/ApPoPickup",
            referenceId: procurement.ProcurementId,
            createdByUserId: null,
            ct: ct
        );
    }

    private async Task SendRejectionNotification(
        Procurement procurement,
        ProcurementStatus rejectedAtStatus,
        string? rejectorUserId,
        string? rejectionNote,
        CancellationToken ct
    )
    {
        var submitterUserId = procurement.ApprovalSentByUserId;
        if (string.IsNullOrEmpty(submitterUserId))
            return;

        var rejectorRole = rejectedAtStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
            ProcurementStatus.WaitingApprovalAsstManager => RoleNames.AsstManager,
            ProcurementStatus.WaitingApprovalManager => "Manager Transport & Logistic",
            ProcurementStatus.WaitingApprovalVP => "Vice President",
            ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
            ProcurementStatus.WaitingApprovalPresDir => "President Director",
            _ => "Approver",
        };
        var message = string.IsNullOrEmpty(rejectionNote)
            ? $"WO: {procurement.Wonum} - Procurement ditolak"
            : $"WO: {procurement.Wonum} - Alasan: {rejectionNote}";

        await _notificationService.SendNotificationAsync(
            userId: submitterUserId,
            title: $"{procurement.DisplayName} ditolak oleh {rejectorRole}",
            message: message,
            notificationType: "PrRejected",
            actionUrl: $"/ProcurementTracking/Details/{procurement.ProcurementId}",
            referenceId: procurement.ProcurementId,
            createdByUserId: rejectorUserId,
            ct: ct
        );
    }
}
