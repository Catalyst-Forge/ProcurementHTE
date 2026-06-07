using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> SendForApprovalAsync(
        string procurementId,
        string sentByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus != ProcurementStatus.OnCreateDP3)
        {
            return Failed(
                $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. Approval hanya bisa dikirim saat status 'On Create DP3'."
            );
        }

        var procIds = new List<string> { procurementId };
        var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(procIds, ct);
        var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);
        if (totalDocs > 0 && uploadedDocs < totalDocs)
            return Failed($"Dokumen wajib belum lengkap. {uploadedDocs}/{totalDocs} dokumen telah diupload.");

        procurement.ApprovalToken = GenerateApprovalToken();
        procurement.ApprovalTokenGeneratedAt = DateTime.UtcNow;
        procurement.ApprovalSentByUserId = sentByUserId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await AssignHigherLevelApproversAsync(procurement, ct);
        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.WaitingApprovalAnalyst,
            sentByUserId,
            "Sent for Analyst HTE approval",
            ct
        );

        procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement != null)
            await SendApprovalNotification(procurement, ct);

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "Procurement berhasil dikirim untuk approval.",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    private async Task AssignHigherLevelApproversAsync(
        Procurement procurement,
        CancellationToken ct
    )
    {
        var ctValue = await GetCtAsync(procurement.ProcurementId);
        var requiredLevel = ApprovalConstants.GetRequiredApprovalLevel(ctValue);

        _logger.LogInformation(
            "Procurement {ProcNum}: CT = {CT:N0}, Required Approval Level = {Level}",
            procurement.ProcNum,
            ctValue,
            requiredLevel
        );

        if (
            requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_VP
            && string.IsNullOrEmpty(procurement.VicePresidentUserId)
        )
            await AssignRoleApproverAsync(procurement, "Vice President", ct);

        if (
            requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_OP_DIR
            && string.IsNullOrEmpty(procurement.OperationDirectorUserId)
        )
            await AssignRoleApproverAsync(procurement, "Operation Director", ct);

        if (
            requiredLevel >= ApprovalConstants.APPROVAL_LEVEL_PRES_DIR
            && string.IsNullOrEmpty(procurement.PresidentDirectorUserId)
        )
            await AssignRoleApproverAsync(procurement, "President Director", ct);
    }

    private async Task AssignRoleApproverAsync(
        Procurement procurement,
        string roleName,
        CancellationToken ct
    )
    {
        var user = await ResolveFirstUserByRoleAsync(roleName, ct);
        if (user == null)
            return;

        if (roleName == "Vice President")
            procurement.VicePresidentUserId = user.UserId;
        else if (roleName == "Operation Director")
            procurement.OperationDirectorUserId = user.UserId;
        else if (roleName == "President Director")
            procurement.PresidentDirectorUserId = user.UserId;

        _logger.LogInformation("Assigned {Role}: {FullName}", roleName, user.FullName);
    }

    private async Task<UserBasicInfo?> ResolveFirstUserByRoleAsync(
        string roleName,
        CancellationToken ct
    )
    {
        var users = await _userRepo.GetUsersByRoleAsync(roleName, ct);
        return users?.FirstOrDefault();
    }
}
