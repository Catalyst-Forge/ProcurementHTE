using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models;

public partial class Procurement
{
    // Foreign Key
    public string? JobTypeId { get; set; }

    /// <summary>
    /// DEPRECATED: Use ProcurementStatus enum instead. Kept for backward compatibility.
    /// </summary>
    public int StatusId { get; set; }

    public string? UserId { get; set; }
    public string? PrId { get; set; }

    // Nav
    [ForeignKey(nameof(JobTypeId))]
    public JobTypes? JobType { get; set; }

    [ForeignKey(nameof(StatusId))]
    public Status? Status { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(PicOpsUserId))]
    public User? PicOpsUser { get; set; }

    [ForeignKey(nameof(AppoUserId))]
    public User? AppoUser { get; set; }

    [ForeignKey(nameof(AnalystHteUserId))]
    public User? AnalystHteUser { get; set; }

    [ForeignKey(nameof(AssistantManagerUserId))]
    public User? AssistantManagerUser { get; set; }

    [ForeignKey(nameof(ManagerUserId))]
    public User? ManagerUser { get; set; }

    [ForeignKey(nameof(VicePresidentUserId))]
    public User? VicePresidentUser { get; set; }

    [ForeignKey(nameof(OperationDirectorUserId))]
    public User? OperationDirectorUser { get; set; }

    [ForeignKey(nameof(PresidentDirectorUserId))]
    public User? PresidentDirectorUser { get; set; }

    [ForeignKey(nameof(AccrualFilledByUserId))]
    public User? AccrualFilledByUser { get; set; }

    [ForeignKey(nameof(ApInvoiceUserId))]
    public User? ApInvoiceUser { get; set; }

    [ForeignKey(nameof(ArUserId))]
    public User? ArUser { get; set; }

    [ForeignKey(nameof(PrId))]
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    // Tracking User Navigation Properties
    [ForeignKey(nameof(IspaSubmittedByUserId))]
    public User? IspaSubmittedByUser { get; set; }

    [ForeignKey(nameof(PoSubmittedByUserId))]
    public User? PoSubmittedByUser { get; set; }

    [ForeignKey(nameof(HardcopySubmittedByUserId))]
    public User? HardcopySubmittedByUser { get; set; }

    [ForeignKey(nameof(RejectedByUserId))]
    public User? RejectedByUser { get; set; }

    [ForeignKey(nameof(ResubmittedByUserId))]
    public User? ResubmittedByUser { get; set; }

    [ForeignKey(nameof(ApprovalSentByUserId))]
    public User? ApprovalSentByUser { get; set; }

    public ICollection<ProcOffer> ProcOffers { get; set; } = [];
    public ICollection<ProcDocuments>? ProcDocuments { get; set; } = [];
    public ICollection<ProcDetail>? ProcDetails { get; set; } = [];
    public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    // DocumentApprovals collection removed - approval moved to procurement-level tracking
    public ICollection<ProfitLoss> ProfitLosses { get; set; } = [];

    /// <summary>
    /// Status history tracking untuk procurement ini
    /// </summary>
    public ICollection<ProcurementStatusHistory> StatusHistories { get; set; } = [];
}
