using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models.DTOs
{
    /// <summary>
    /// DTO untuk tracking Procurement individual dengan progress bar
    /// </summary>
    public class ProcurementTrackingDto
    {
        public string ProcurementId { get; set; } = null!;
        public string ProcNum { get; set; } = null!;
        public string? Wonum { get; set; }
        public string JobName { get; set; } = null!;
        public DateTime DocumentDate { get; set; }
        public ProcurementStatus CurrentStatus { get; set; }
        public string CurrentStatusDescription { get; set; } = null!;

        // Project Region (SMTR / JWKT / AKOMODASI)
        public ProjectRegion ProjectRegion { get; set; }

        // Parent PR Info
        public string? PrId { get; set; }
        public string? PrNumber { get; set; }

        // ISPA Info
        public string? IspaNumber { get; set; }
        public DateTime? IspaSubmittedAt { get; set; }
        public string? IspaSubmittedByUserName { get; set; }

        // PO Info
        public string? PoNumber { get; set; }
        public DateTime? PoSubmittedAt { get; set; }
        public string? PoSubmittedByUserName { get; set; }

        // Hardcopy Evidence Info
        public string? HardcopyEvidenceFileName { get; set; }
        public string? HardcopyEvidenceFilePath { get; set; }
        public DateTime? HardcopySubmittedAt { get; set; }
        public string? HardcopySubmittedByUserName { get; set; }

        // Rejection Info
        public string? RejectionNote { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedByUserName { get; set; }

        // Approval Token Info
        public string? ApprovalToken { get; set; }
        public DateTime? ApprovalTokenGeneratedAt { get; set; }
        public string? ApprovalSentByUserName { get; set; }

        // Approver Info
        public string? AppoUserId { get; set; }
        public string? AnalystHteUserName { get; set; }
        public string? AssistantManagerUserName { get; set; }
        public string? ManagerUserName { get; set; }
        public string? VicePresidentUserName { get; set; }
        public string? OperationDirectorUserName { get; set; }
        public string? PresidentDirectorUserName { get; set; }

        // Pjs (Penanggung Jawab Sementara / Acting) Flags
        public bool AnalystHtePjs { get; set; }
        public bool AssistantManagerPjs { get; set; }
        public bool ManagerPjs { get; set; }
        public bool VicePresidentPjs { get; set; }
        public bool OperationDirectorPjs { get; set; }
        public bool PresidentDirectorPjs { get; set; }

        // Contract Total (Final Offer from PNL) - used for dynamic approval
        public decimal? FinalOfferPnl { get; set; }

        /// <summary>
        /// Required approval level based on CT (4=Manager, 5=VP, 6=OpDir, 7=PresDir)
        /// </summary>
        public int RequiredApprovalLevel { get; set; } = 4;

        // Timeline/History
        public List<ProcurementStatusHistoryDto> StatusHistory { get; set; } = new();

        // Progress Calculation - Dynamic based on RequiredApprovalLevel
        // Status values: 1-4 = base approvals, 5-7 = higher approvals, 8-11 = post-approval steps
        public int ProgressStep
        {
            get
            {
                if (CurrentStatus == ProcurementStatus.Rejected)
                    return 0;

                int statusValue = (int)CurrentStatus;
                
                // For statuses after approval (ISPA=8, Hardcopy=9, PO=10, Done=11)
                // We need to adjust based on which approval levels were skipped
                if (statusValue >= 8)
                {
                    // Calculate how many approval levels were skipped
                    // Default is 4 (Manager), max is 7 (PresDir)
                    int skippedLevels = 7 - RequiredApprovalLevel;
                    return statusValue - skippedLevels;
                }

                // For approval statuses (1-7), check if they're beyond required level
                if (statusValue > RequiredApprovalLevel)
                {
                    // This shouldn't happen in normal flow, but handle gracefully
                    return RequiredApprovalLevel;
                }

                return statusValue;
            }
        }

        /// <summary>
        /// Total steps based on required approval level
        /// Base: 8 steps (Create + 3 approvals + ISPA + Hardcopy + PO + Done)
        /// With VP: 9, With OpDir: 10, With PresDir: 11
        /// </summary>
        public int TotalSteps => Constants.ApprovalConstants.GetTotalSteps(RequiredApprovalLevel);

        public decimal ProgressPercentage => CurrentStatus == ProcurementStatus.Rejected
            ? 0
            : TotalSteps > 0 ? ((decimal)ProgressStep / TotalSteps) * 100 : 0;

        // Mandatory Documents Status (per procurement)
        public int TotalMandatoryDocs { get; set; }
        public int UploadedMandatoryDocs { get; set; }
        public int MissingMandatoryDocs => TotalMandatoryDocs - UploadedMandatoryDocs;
        public bool AllMandatoryDocsReady => TotalMandatoryDocs > 0 && MissingMandatoryDocs == 0;

        // Can Send Approval - All conditions must be met
        public bool CanSendApproval => CurrentStatus == ProcurementStatus.OnCreateDP3
                                       && AllMandatoryDocsReady;

        // Approval QR Code - URL for approval via QR scan
        public string? ApprovalQrUrl { get; set; }

        // Next Approver Info
        public string? NextApproverRole { get; set; }
        public string? NextApproverName { get; set; }

        // Approval Status Info - includes all approval levels
        public bool IsWaitingApproval => CurrentStatus == ProcurementStatus.WaitingApprovalAnalyst
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalAsstManager
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalManager
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalVP
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalOpDir
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalPresDir;

        // Total documents count (for this procurement)
        public int TotalDocuments { get; set; }

        // Profit & Loss Summary
        public PnLSummaryCardDto? PnLSummary { get; set; }

        // ISPA Date Fields
        public DateTime? IspaDate { get; set; }
        public DateTime? IspaSubmitDate { get; set; }

        // ISPA File Info
        public string? IspaFileName { get; set; }
        public string? IspaFileObjectKey { get; set; }

        // Revision Tracking Fields
        public RejectionSymptom? RejectionSymptoms { get; set; }
        public RejectionSymptom? PendingRevisionSymptoms { get; set; }
        public ProcurementStatus? StatusBeforeRejection { get; set; }
        public int RevisionCount { get; set; }
        public DateTime? ResubmittedAt { get; set; }
        public string? ResubmittedByUserName { get; set; }

        // PIC Ops Info (for revision tracking)
        public string? PicOpsUserId { get; set; }
        public string? PicOpsUserName { get; set; }
    }

    /// <summary>
    /// DTO untuk ringkasan Profit & Loss di halaman tracking
    /// </summary>
    public class PnLSummaryCardDto
    {
        public string? ProfitLossId { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal FinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public string? SelectedVendorName { get; set; }
        public bool HasData => !string.IsNullOrEmpty(ProfitLossId);
    }

    /// <summary>
    /// DTO untuk status history item procurement
    /// </summary>
    public class ProcurementStatusHistoryDto
    {
        public string Id { get; set; } = null!;
        public ProcurementStatus Status { get; set; }
        public string StatusDescription { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? ChangedByUserName { get; set; }
        public string? ChangedByFullName { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request DTO untuk submit ISPA per procurement
    /// </summary>
    public class SubmitProcurementIspaRequest
    {
        public string ProcurementId { get; set; } = null!;
        public string IspaNumber { get; set; } = null!;
    }

    /// <summary>
    /// Request DTO untuk submit PO per procurement
    /// </summary>
    public class SubmitProcurementPoRequest
    {
        public string ProcurementId { get; set; } = null!;
        public string PoNumber { get; set; } = null!;
    }

    /// <summary>
    /// Request DTO untuk submit justification & hardcopy evidence per procurement
    /// </summary>
    public class SubmitProcurementJustificationRequest
    {
        public string ProcurementId { get; set; } = null!;
        public string HardcopyEvidenceFileName { get; set; } = null!;
        public string HardcopyEvidenceFilePath { get; set; } = null!;
        public string HardcopyEvidenceContentType { get; set; } = null!;
        public long HardcopyEvidenceFileSize { get; set; }
    }

    /// <summary>
    /// Response DTO untuk procurement tracking operations
    /// </summary>
    public class ProcurementTrackingResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ProcurementTrackingDto? Data { get; set; }
    }

    /// <summary>
    /// DTO untuk PR summary dengan list of procurements
    /// </summary>
    public class PRWithProcurementsTrackingDto
    {
        // PR Summary Info
        public string PrId { get; set; } = null!;
        public string PrNumber { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public string? Description { get; set; }
        public PurchaseRequisitionStatus CurrentStatus { get; set; }
        public string CurrentStatusDescription { get; set; } = null!;

        // List of linked procurements with their tracking status
        public List<ProcurementTrackingDto> Procurements { get; set; } = new();

        // Aggregate statistics
        public int TotalProcurements => Procurements.Count;
        public int CompletedProcurements => Procurements.Count(p => p.CurrentStatus == ProcurementStatus.DonePO);
        public int RejectedProcurements => Procurements.Count(p => p.CurrentStatus == ProcurementStatus.Rejected);
        public int InProgressProcurements => TotalProcurements - CompletedProcurements - RejectedProcurements;

        // Overall progress
        public decimal OverallProgressPercentage => TotalProcurements == 0
            ? 0
            : ((decimal)CompletedProcurements / TotalProcurements) * 100;
    }
}
