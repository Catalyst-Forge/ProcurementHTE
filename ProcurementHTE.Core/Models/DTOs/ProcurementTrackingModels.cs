using System;
using System.Collections.Generic;
using System.Linq;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models.DTOs
{
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
