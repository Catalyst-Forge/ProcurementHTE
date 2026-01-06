using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models.DTOs
{
    /// <summary>
    /// DTO untuk tracking Purchase Requisition dengan progress bar
    /// </summary>
    public class PRTrackingDto
    {
        public string PrId { get; set; } = null!;
        public string PrNumber { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public string? Description { get; set; }
        public PurchaseRequisitionStatus CurrentStatus { get; set; }
        public string CurrentStatusDescription { get; set; } = null!;

        // ISPA Info
        public string? IspaNumber { get; set; }
        public DateTime? IspaSubmittedAt { get; set; }
        public string? IspaSubmittedByUserName { get; set; }

        // PO Info
        public string? PoNumber { get; set; }
        public DateTime? PoSubmittedAt { get; set; }
        public string? PoSubmittedByUserName { get; set; }

        // Rejection Info
        public string? RejectionNote { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectedByUserName { get; set; }

        // Created By Info
        public string? CreatedByUserName { get; set; }
        public string? CreatedByFullName { get; set; }

        // Timeline/History
        public List<PRStatusHistoryDto> StatusHistory { get; set; } = new();

        // Progress Calculation (1-8 for normal flow, 99 for rejected)
        public int ProgressStep => CurrentStatus == PurchaseRequisitionStatus.Rejected
            ? 0
            : (int)CurrentStatus;
        public int TotalSteps => 8;
        public decimal ProgressPercentage => CurrentStatus == PurchaseRequisitionStatus.Rejected
            ? 0
            : ((decimal)ProgressStep / TotalSteps) * 100;

        // Linked Procurements Info
        public int LinkedProcurementsCount { get; set; }

        // Mandatory Documents Status
        public int TotalMandatoryDocs { get; set; }
        public int UploadedMandatoryDocs { get; set; }
        public int MissingMandatoryDocs => TotalMandatoryDocs - UploadedMandatoryDocs;
        public bool AllMandatoryDocsReady => TotalMandatoryDocs > 0 && MissingMandatoryDocs == 0;

        // Can Send Approval - All conditions must be met
        public bool CanSendApproval => CurrentStatus == PurchaseRequisitionStatus.OnCreateDP3
                                       && LinkedProcurementsCount > 0
                                       && AllMandatoryDocsReady;

        // Approval QR Code - URL for approval via QR scan
        public string? ApprovalQrUrl { get; set; }

        // Next Approver Info
        public string? NextApproverRole { get; set; }
        public string? NextApproverName { get; set; }

        // Approval Status Info
        public bool IsWaitingApproval => CurrentStatus == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                                         || CurrentStatus == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                                         || CurrentStatus == PurchaseRequisitionStatus.WaitingApprovalManager;

        // Documents pending approval count
        public int DocumentsPendingApproval { get; set; }
        public int DocumentsApproved { get; set; }
    }

    /// <summary>
    /// DTO untuk status history item
    /// </summary>
    public class PRStatusHistoryDto
    {
        public string Id { get; set; } = null!;
        public PurchaseRequisitionStatus Status { get; set; }
        public string StatusDescription { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? ChangedByUserName { get; set; }
        public string? ChangedByFullName { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request DTO untuk submit ISPA
    /// </summary>
    public class SubmitIspaRequest
    {
        public string PrId { get; set; } = null!;
        public string IspaNumber { get; set; } = null!;
    }

    /// <summary>
    /// Request DTO untuk submit PO
    /// </summary>
    public class SubmitPoRequest
    {
        public string PrId { get; set; } = null!;
        public string PoNumber { get; set; } = null!;
    }

    /// <summary>
    /// Response DTO untuk tracking operations
    /// </summary>
    public class PRTrackingResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public PRTrackingDto? Data { get; set; }
    }
}
