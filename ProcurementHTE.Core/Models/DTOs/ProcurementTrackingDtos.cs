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

}
