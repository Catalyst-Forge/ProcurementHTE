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

        // Timeline/History
        public List<ProcurementStatusHistoryDto> StatusHistory { get; set; } = new();

        // Progress Calculation (1-8 for normal flow, 99 for rejected)
        public int ProgressStep => CurrentStatus == ProcurementStatus.Rejected
            ? 0
            : (int)CurrentStatus;
        public int TotalSteps => 8;
        public decimal ProgressPercentage => CurrentStatus == ProcurementStatus.Rejected
            ? 0
            : ((decimal)ProgressStep / TotalSteps) * 100;

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

        // Approval Status Info
        public bool IsWaitingApproval => CurrentStatus == ProcurementStatus.WaitingApprovalAnalyst
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalAsstManager
                                         || CurrentStatus == ProcurementStatus.WaitingApprovalManager;

        // Total documents count (for this procurement)
        public int TotalDocuments { get; set; }
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
