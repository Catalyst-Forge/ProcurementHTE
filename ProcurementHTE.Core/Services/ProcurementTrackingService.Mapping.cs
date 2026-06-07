using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private ProcurementTrackingDto MapToDto(
        Procurement procurement,
        Dictionary<string, bool> needsJustifikasiMap
    )
    {
        var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);
        var pnlSummary = BuildPnlSummary(procurement);

        return new ProcurementTrackingDto
        {
            ProcurementId = procurement.ProcurementId,
            ProcNum = procurement.ProcNum ?? string.Empty,
            Wonum = procurement.Wonum,
            JobName = procurement.JobName ?? string.Empty,
            DocumentDate = procurement.DocumentDate,
            CurrentStatus = procurement.ProcurementStatus,
            CurrentStatusDescription = GetProcurementStatusDescription(procurement.ProcurementStatus),
            ProjectRegion = procurement.ProjectRegion,
            PrId = procurement.PrId,
            PrNumber = procurement.PurchaseRequisition?.PrNumber,
            IspaNumber = procurement.IspaNumber,
            IspaSubmittedAt = procurement.IspaSubmittedAt,
            IspaSubmittedByUserName = procurement.IspaSubmittedByUser?.FullName,
            IspaDate = procurement.IspaDate,
            IspaSubmitDate = procurement.IspaSubmitDate,
            IspaFileName = procurement.IspaFileName,
            IspaFileObjectKey = procurement.IspaFileObjectKey,
            PoNumber = procurement.PoNumber,
            PoSubmittedAt = procurement.PoSubmittedAt,
            PoSubmittedByUserName = procurement.PoSubmittedByUser?.FullName,
            HardcopyEvidenceFileName = procurement.HardcopyEvidenceFileName,
            HardcopyEvidenceFilePath = procurement.HardcopyEvidenceFilePath,
            HardcopySubmittedAt = procurement.HardcopySubmittedAt,
            HardcopySubmittedByUserName = procurement.HardcopySubmittedByUser?.FullName,
            RejectionNote = procurement.RejectionNote,
            RejectedAt = procurement.RejectedAt,
            RejectedByUserName = procurement.RejectedByUser?.FullName,
            ApprovalToken = procurement.ApprovalToken,
            ApprovalTokenGeneratedAt = procurement.ApprovalTokenGeneratedAt,
            ApprovalSentByUserName = procurement.ApprovalSentByUser?.FullName,
            AppoUserId = procurement.AppoUserId,
            AnalystHteUserName = procurement.AnalystHteUser?.FullName,
            AssistantManagerUserName = procurement.AssistantManagerUser?.FullName,
            ManagerUserName = procurement.ManagerUser?.FullName,
            VicePresidentUserName = procurement.VicePresidentUser?.FullName,
            OperationDirectorUserName = procurement.OperationDirectorUser?.FullName,
            PresidentDirectorUserName = procurement.PresidentDirectorUser?.FullName,
            AnalystHtePjs = procurement.AnalystHtePjs,
            AssistantManagerPjs = procurement.AssistantManagerPjs,
            ManagerPjs = procurement.ManagerPjs,
            VicePresidentPjs = procurement.VicePresidentPjs,
            OperationDirectorPjs = procurement.OperationDirectorPjs,
            PresidentDirectorPjs = procurement.PresidentDirectorPjs,
            FinalOfferPnl = pnlSummary?.FinalOffer,
            RequiredApprovalLevel = ApprovalConstants.GetRequiredApprovalLevel(pnlSummary?.FinalOffer ?? 0),
            StatusHistory = procurement.StatusHistories?.Select(MapToHistoryDto).ToList() ?? [],
            TotalMandatoryDocs = totalDocs,
            UploadedMandatoryDocs = uploadedDocs,
            TotalDocuments = procurement.ProcDocuments?.Count ?? 0,
            ApprovalQrUrl = !string.IsNullOrEmpty(procurement.ApprovalToken)
                ? GenerateQrCodeDataUri(procurement.ApprovalToken)
                : null,
            NextApproverRole = GetNextApproverRole(procurement.ProcurementStatus),
            NextApproverName = GetNextApproverName(procurement),
            PnLSummary = pnlSummary,
            RejectionSymptoms = procurement.RejectionSymptoms,
            PendingRevisionSymptoms = procurement.PendingRevisionSymptoms,
            StatusBeforeRejection = procurement.StatusBeforeRejection,
            RevisionCount = procurement.RevisionCount,
            ResubmittedAt = procurement.ResubmittedAt,
            ResubmittedByUserName = procurement.ResubmittedByUser?.FullName,
            PicOpsUserId = procurement.PicOpsUserId,
            PicOpsUserName = procurement.PicOpsUser?.FullName,
        };
    }

    private static PnLSummaryCardDto? BuildPnlSummary(Procurement procurement)
    {
        var profitLoss = procurement.ProfitLosses?.FirstOrDefault();
        if (profitLoss == null)
            return null;

        return new PnLSummaryCardDto
        {
            ProfitLossId = profitLoss.ProfitLossId,
            TotalRevenue = profitLoss.Items?.Sum(i => i.Revenue) ?? 0,
            FinalOffer = profitLoss.SelectedVendorFinalOffer,
            Profit = profitLoss.Profit,
            ProfitMarginPercent = profitLoss.ProfitPercent,
            SelectedVendorName = profitLoss.SelectedVendor?.VendorName,
        };
    }

    private static ProcurementStatusHistoryDto MapToHistoryDto(ProcurementStatusHistory history)
    {
        return new ProcurementStatusHistoryDto
        {
            Id = history.Id,
            Status = history.Status,
            StatusDescription = GetProcurementStatusDescription(history.Status),
            ChangedAt = history.ChangedAt,
            ChangedByUserName = history.ChangedByUser?.UserName,
            ChangedByFullName = history.ChangedByUser?.FullName,
            Note = history.Note,
        };
    }
}
