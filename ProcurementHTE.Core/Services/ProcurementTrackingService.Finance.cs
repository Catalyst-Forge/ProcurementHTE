using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private const string ApPoRole = "AP-PO";

    /// <summary>
    /// Saves one cell of a finance table. After the approvals, the AP-PO cells also move the job
    /// through the INPRO, Hard Copy, and PO steps.
    /// </summary>
    public async Task<FinanceFieldChange> UpdateFinanceFieldAsync(
        string procurementId,
        FinanceField field,
        string? value,
        string changedByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return new(false, "Pekerjaan tidak ditemukan.", string.Empty);

        var change = FinanceTable.Apply(procurement, field, value);
        if (!change.Success)
            return change;

        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepo.UpdateProcurementAsync(procurement);

        _logger.LogInformation(
            "Procurement {ProcurementId} {Field} set to {Value} by user {UserId}",
            procurementId,
            field,
            change.Display,
            changedByUserId
        );

        await AdvanceFinanceStageAsync(procurement, changedByUserId, ct);
        return change;
    }

    /// <summary>
    /// AP-PO's approval button: the first send goes to the Analyst, and each later one hands the
    /// job to the level that is next after the last approver signed.
    /// </summary>
    public async Task<ProcurementTrackingResponse> SendApprovalStepAsync(
        string procurementId,
        string sentByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus == Enums.ProcurementStatus.OnCreateDP3)
            return await SendForApprovalAsync(procurementId, sentByUserId, ct);

        var target = FinanceTable.ApproverFor(procurement.ProcurementStatus);
        if (!procurement.ApprovalAwaitingSend || target == null)
            return Failed("Tidak ada approval yang menunggu dikirim untuk procurement ini.");

        procurement.ApprovalAwaitingSend = false;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepo.UpdateProcurementAsync(procurement);
        await SendApprovalNotification(procurement, ct);

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = $"Approval {procurement.DisplayName} dikirim ke {target}.",
        };
    }

    /// <summary>Moves a fully approved job to the step its cells say it is at.</summary>
    private async Task AdvanceFinanceStageAsync(Procurement procurement, string? userId, CancellationToken ct)
    {
        var stage = FinanceTable.StageAfterApproval(procurement);
        if (stage is null || stage == procurement.ProcurementStatus)
            return;

        await UpdateProcurementStatusAsync(
            procurement.ProcurementId,
            stage.Value,
            userId,
            "Isian AP-PO diperbarui",
            ct
        );
    }

    /// <summary>Every AP-PO user hears about the job, since nobody picks it up any more.</summary>
    private async Task NotifyApPoUsersAsync(
        string title,
        string message,
        string notificationType,
        string actionUrl,
        string referenceId,
        string? createdByUserId,
        CancellationToken ct
    )
    {
        var userIds = await _userRepo.GetUserIdsByRoleAsync(ApPoRole, ct);
        foreach (var userId in userIds.Distinct())
        {
            await _notificationService.SendNotificationAsync(
                userId: userId,
                title: title,
                message: message,
                notificationType: notificationType,
                actionUrl: actionUrl,
                referenceId: referenceId,
                createdByUserId: createdByUserId,
                ct: ct
            );
        }
    }
}
