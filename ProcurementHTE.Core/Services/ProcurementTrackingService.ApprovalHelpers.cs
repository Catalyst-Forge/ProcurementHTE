using System.ComponentModel;
using System.Reflection;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private static string GenerateApprovalToken()
    {
        var shortGuid = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"APPR-{shortGuid}-{timestamp}";
    }

    private string GenerateQrCodeDataUri(string approvalToken)
    {
        var deepLink = $"procurehte://approve/{approvalToken}";
        return _qrCodeGenerator.GenerateAsDataUri(deepLink, 10);
    }

    private static string? GetNextApproverRole(ProcurementStatus status)
    {
        return status switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
            ProcurementStatus.WaitingApprovalAsstManager => "Assistant Manager",
            ProcurementStatus.WaitingApprovalManager => "Manager",
            ProcurementStatus.WaitingApprovalVP => "Vice President",
            ProcurementStatus.WaitingApprovalOpDir => "Operation Director",
            ProcurementStatus.WaitingApprovalPresDir => "President Director",
            _ => null,
        };
    }

    private static string? GetNextApproverName(Procurement procurement)
    {
        return procurement.ProcurementStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => procurement.AnalystHteUserId,
            ProcurementStatus.WaitingApprovalAsstManager => procurement.AssistantManagerUserId,
            ProcurementStatus.WaitingApprovalManager => procurement.ManagerUserId,
            ProcurementStatus.WaitingApprovalVP => procurement.VicePresidentUserId,
            ProcurementStatus.WaitingApprovalOpDir => procurement.OperationDirectorUserId,
            ProcurementStatus.WaitingApprovalPresDir => procurement.PresidentDirectorUserId,
            _ => null,
        };
    }

    private async Task<decimal> GetCtAsync(string procurementId)
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        var profitLoss = procurement?.ProfitLosses?.FirstOrDefault();
        return profitLoss?.SelectedVendorFinalOffer ?? 0m;
    }

    private static ProcurementStatus GetNextApprovalStatus(
        ProcurementStatus currentStatus,
        int requiredLevel
    )
    {
        return currentStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => ProcurementStatus.WaitingApprovalAsstManager,
            ProcurementStatus.WaitingApprovalAsstManager => ProcurementStatus.WaitingApprovalManager,
            ProcurementStatus.WaitingApprovalManager => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_VP
                ? ProcurementStatus.WaitingApprovalVP
                : ProcurementStatus.OnSubmitISPA,
            ProcurementStatus.WaitingApprovalVP => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_OP_DIR
                ? ProcurementStatus.WaitingApprovalOpDir
                : ProcurementStatus.OnSubmitISPA,
            ProcurementStatus.WaitingApprovalOpDir => requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_PRES_DIR
                ? ProcurementStatus.WaitingApprovalPresDir
                : ProcurementStatus.OnSubmitISPA,
            ProcurementStatus.WaitingApprovalPresDir => ProcurementStatus.OnSubmitISPA,
            _ => currentStatus,
        };
    }

    private static void SetApprovalTimelineOnStatusChange(
        Procurement procurement,
        ProcurementStatus fromStatus,
        ProcurementStatus toStatus,
        DateTime timestamp
    )
    {
        switch (fromStatus)
        {
            case ProcurementStatus.WaitingApprovalManager:
                procurement.ManagerApprovalEndAt = timestamp;
                break;
            case ProcurementStatus.WaitingApprovalVP:
                procurement.VpApprovalEndAt = timestamp;
                break;
            case ProcurementStatus.WaitingApprovalOpDir:
                procurement.OpDirApprovalEndAt = timestamp;
                break;
            case ProcurementStatus.WaitingApprovalPresDir:
                procurement.PresDirApprovalEndAt = timestamp;
                break;
        }

        switch (toStatus)
        {
            case ProcurementStatus.WaitingApprovalManager:
                procurement.ManagerApprovalStartAt ??= timestamp;
                break;
            case ProcurementStatus.WaitingApprovalVP:
                procurement.VpApprovalStartAt ??= timestamp;
                break;
            case ProcurementStatus.WaitingApprovalOpDir:
                procurement.OpDirApprovalStartAt ??= timestamp;
                break;
            case ProcurementStatus.WaitingApprovalPresDir:
                procurement.PresDirApprovalStartAt ??= timestamp;
                break;
        }
    }

    private static string GetStatusDescription(PurchaseRequisitionStatus status) =>
        GetEnumDescription(status);

    private static string GetProcurementStatusDescription(ProcurementStatus status) =>
        GetEnumDescription(status);

    private static string GetEnumDescription<TEnum>(TEnum status)
        where TEnum : struct, Enum
    {
        var field = status.GetType().GetField(status.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? status.ToString();
    }
}
