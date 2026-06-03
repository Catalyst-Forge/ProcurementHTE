using ProcurementHTE.Core.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("PurchaseRequisitions")]
public class PurchaseRequisition : BaseEntity
{
    [Key]
    public string PrId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    [DisplayName("PR Number")]
    public string PrNumber { get; set; } = null!;

    [Required]
    [DisplayName("Request Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime RequestDate { get; set; }

    [MaxLength(1000)]
    [DisplayName("Description")]
    public string? Description { get; set; }

    [MaxLength(255)]
    [DisplayName("Document File Name")]
    public string? DocumentFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("Document File Path")]
    public string? DocumentFilePath { get; set; }

    [MaxLength(100)]
    [DisplayName("Document Content Type")]
    public string? DocumentContentType { get; set; }

    [DisplayName("Document File Size")]
    public long? DocumentFileSize { get; set; }

    [Required]
    [MaxLength(450)]
    [DisplayName("Created By")]
    public string CreatedByUserId { get; set; } = null!;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DisplayName("Updated At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? UpdatedAt { get; set; }

    // PR Tracking Status Fields
    /// <summary>
    /// Status stored in database. Will be automatically updated from DerivedStatus.
    /// For display/logic, use DerivedStatus property instead.
    /// </summary>
    [Required]
    [DisplayName("Status")]
    public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.OnCreateDP3;

    /// <summary>
    /// Derived/calculated status based on linked procurements.
    /// Logic:
    /// - If no procurements: return current Status
    /// - If any procurement Rejected: PR = Rejected
    /// - If all procurements DonePO: PR = DonePO
    /// - Otherwise: return earliest (minimum) procurement status
    /// </summary>
    [NotMapped]
    [DisplayName("Derived Status")]
    public PurchaseRequisitionStatus DerivedStatus
    {
        get
        {
            // If no procurements linked, use current status
            if (Procurements == null || !Procurements.Any())
                return Status;

            // Any rejected → PR rejected
            if (Procurements.Any(p => p.ProcurementStatus == ProcurementStatus.Rejected))
                return PurchaseRequisitionStatus.Rejected;

            // All done → PR done
            if (Procurements.All(p => p.ProcurementStatus == ProcurementStatus.DonePO))
                return PurchaseRequisitionStatus.DonePO;

            // Return minimum (earliest) status from procurements
            var minStatus = Procurements.Min(p => (int)p.ProcurementStatus);
            return (PurchaseRequisitionStatus)minStatus;
        }
    }

    [MaxLength(100)]
    [DisplayName("ISPA Number")]
    public string? IspaNumber { get; set; }

    [DisplayName("ISPA Submitted At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? IspaSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("ISPA Submitted By")]
    public string? IspaSubmittedByUserId { get; set; }

    [MaxLength(100)]
    [DisplayName("PO Number")]
    public string? PoNumber { get; set; }

    [DisplayName("PO Submitted At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? PoSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("PO Submitted By")]
    public string? PoSubmittedByUserId { get; set; }

    // Hardcopy Evidence Photo Fields
    [MaxLength(255)]
    [DisplayName("Hardcopy Evidence File Name")]
    public string? HardcopyEvidenceFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("Hardcopy Evidence File Path")]
    public string? HardcopyEvidenceFilePath { get; set; }

    [MaxLength(100)]
    [DisplayName("Hardcopy Evidence Content Type")]
    public string? HardcopyEvidenceContentType { get; set; }

    [DisplayName("Hardcopy Evidence File Size")]
    public long? HardcopyEvidenceFileSize { get; set; }

    [DisplayName("Hardcopy Submitted At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? HardcopySubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Hardcopy Submitted By")]
    public string? HardcopySubmittedByUserId { get; set; }

    [MaxLength(1000)]
    [DisplayName("Rejection Note")]
    public string? RejectionNote { get; set; }

    [DisplayName("Rejected At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? RejectedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Rejected By")]
    public string? RejectedByUserId { get; set; }

    // Approval QR Code Fields
    [MaxLength(100)]
    [DisplayName("Approval Token")]
    public string? ApprovalToken { get; set; }

    [DisplayName("Approval Token Generated At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? ApprovalTokenGeneratedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Approval Sent By")]
    public string? ApprovalSentByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(IspaSubmittedByUserId))]
    public User? IspaSubmittedByUser { get; set; }

    [ForeignKey(nameof(PoSubmittedByUserId))]
    public User? PoSubmittedByUser { get; set; }

    [ForeignKey(nameof(HardcopySubmittedByUserId))]
    public User? HardcopySubmittedByUser { get; set; }

    [ForeignKey(nameof(RejectedByUserId))]
    public User? RejectedByUser { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    [ForeignKey(nameof(ApprovalSentByUserId))]
    public User? ApprovalSentByUser { get; set; }

    public ICollection<Procurement> Procurements { get; set; } = [];
    public ICollection<PurchaseRequisitionStatusHistory> StatusHistories { get; set; } = [];
}
