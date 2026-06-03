using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models;

public class Procurement : BaseEntity
{
    [Key]
    public string ProcurementId { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    [DisplayName("Procurement No.")]
    public string? ProcNum { get; set; }

    [MaxLength(100)]
    [DisplayName("SPK Number")]
    public string? SpkNumber { get; set; }

    [MaxLength(100)]
    [DisplayName("WO Number")]
    public string? Wonum { get; set; }

    [DisplayName("Contract Type")]
    public ContractType ContractType { get; set; }

    [MaxLength(255)]
    [DisplayName("Job Name")]
    public string? JobName { get; set; }

    [DisplayName("Document Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime DocumentDate { get; set; }

    [DisplayName("Start Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime StartDate { get; set; }

    [DisplayName("End Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime EndDate { get; set; }

    [DisplayName("Project Region")]
    public ProjectRegion ProjectRegion { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Potential Accrual Date")]
    public DateTime? PotentialAccrualDate { get; set; }

    [MaxLength(100)]
    [DisplayName("SPMP Number")]
    public string? SpmpNumber { get; set; }

    [MaxLength(100)]
    [DisplayName("Memo Number")]
    public string? MemoNumber { get; set; }

    [MaxLength(100)]
    [DisplayName("OE Number")]
    public string? OeNumber { get; set; }

    [MaxLength(100)]
    [DisplayName("RA Number")]
    public string? RaNumber { get; set; }

    [MaxLength(64)]
    [DisplayName("Project Code")]
    public string? ProjectCode { get; set; }

    [MaxLength(255)]
    [DisplayName("LTC Name")]
    public string? LtcName { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    [MaxLength(450)]
    [DisplayName("PIC User")]
    public string? PicOpsUserId { get; set; }

    [MaxLength(450)]
    [DisplayName("Analyst HTE User")]
    public string? AnalystHteUserId { get; set; }

    [MaxLength(450)]
    [DisplayName("Assistant Manager User")]
    public string? AssistantManagerUserId { get; set; }

    [MaxLength(450)]
    [DisplayName("Manager User")]
    public string? ManagerUserId { get; set; }

    // Higher Level Approvers (assigned dynamically based on CT value)
    [MaxLength(450)]
    [DisplayName("Vice President User")]
    public string? VicePresidentUserId { get; set; }

    [MaxLength(450)]
    [DisplayName("Operation Director User")]
    public string? OperationDirectorUserId { get; set; }

    [MaxLength(450)]
    [DisplayName("President Director User")]
    public string? PresidentDirectorUserId { get; set; }

    // Pjs (Penanggung Jawab Sementara / Acting) flags
    [DisplayName("Analyst HTE Pjs")]
    public bool AnalystHtePjs { get; set; }

    [DisplayName("Assistant Manager Pjs")]
    public bool AssistantManagerPjs { get; set; }

    [DisplayName("Manager Pjs")]
    public bool ManagerPjs { get; set; }

    [DisplayName("Vice President Pjs")]
    public bool VicePresidentPjs { get; set; }

    [DisplayName("Operation Director Pjs")]
    public bool OperationDirectorPjs { get; set; }

    [DisplayName("President Director Pjs")]
    public bool PresidentDirectorPjs { get; set; }

    [MaxLength(450)]
    [DisplayName("AP-PO User")]
    public string? AppoUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Picked Up At")]
    public DateTime? PickedUpAt { get; set; }

    [DisplayName("Jenis Pengadaan")]
    public ProcurementCategory ProcurementCategory { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [DisplayName("Update At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? UpdatedAt { get; set; }

    [DisplayName("Completed At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? CompletedAt { get; set; }

    // Rig & HTE Number (diisi saat create procurement)
    [MaxLength(100)]
    [DisplayName("No. Rig")]
    public string? NoRig { get; set; }

    [MaxLength(100)]
    [DisplayName("No. HTE")]
    public string? NoHte { get; set; }

    // Accrual Fields (diisi oleh AR)
    [MaxLength(100)]
    [DisplayName("No. Accrual")]
    public string? NoAccrual { get; set; }

    [DisplayName("Potensi Accrual")]
    public decimal? PotensiAccrual { get; set; }

    [MaxLength(100)]
    [DisplayName("Status Accrual")]
    public string? StatusAccrual { get; set; }

    [MaxLength(450)]
    [DisplayName("Accrual Filled By")]
    public string? AccrualFilledByUserId { get; set; }

    [DisplayName("Accrual Filled At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? AccrualFilledAt { get; set; }

    // ===== Invoice Fields (filled by AP-Invoice role) =====

    [MaxLength(100)]
    [DisplayName("SA No")]
    public string? SANo { get; set; }

    [MaxLength(100)]
    [DisplayName("SP 3 No")]
    public string? SP3No { get; set; }

    [MaxLength(450)]
    [DisplayName("AP-Invoice User")]
    public string? ApInvoiceUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("AP-Invoice Picked Up At")]
    public DateTime? ApInvoicePickedUpAt { get; set; }

    // ===== AR Pickup Fields =====

    [MaxLength(450)]
    [DisplayName("AR User")]
    public string? ArUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("AR Picked Up At")]
    public DateTime? ArPickedUpAt { get; set; }

    // ===== Procurement-Level Tracking Fields (NEW) =====

    /// <summary>
    /// Status tracking untuk procurement individual (menggantikan StatusId)
    /// </summary>
    [DisplayName("Procurement Status")]
    public ProcurementStatus ProcurementStatus { get; set; } = ProcurementStatus.OnCreateDP3;

    // ISPA Tracking Fields
    [MaxLength(100)]
    [DisplayName("ISPA Number")]
    public string? IspaNumber { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Tanggal ISPA")]
    public DateTime? IspaDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Tanggal Submit ISPA")]
    public DateTime? IspaSubmitDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("ISPA Submitted At")]
    public DateTime? IspaSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("ISPA Submitted By")]
    public string? IspaSubmittedByUserId { get; set; }

    // ISPA File Fields
    [MaxLength(255)]
    [DisplayName("ISPA File Name")]
    public string? IspaFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("ISPA File Path")]
    public string? IspaFileObjectKey { get; set; }

    [MaxLength(100)]
    [DisplayName("ISPA File Content Type")]
    public string? IspaFileContentType { get; set; }

    [DisplayName("ISPA File Size")]
    public long? IspaFileSize { get; set; }

    // PO Tracking Fields
    [MaxLength(100)]
    [DisplayName("PO Number")]
    public string? PoNumber { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("PO Submitted At")]
    public DateTime? PoSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("PO Submitted By")]
    public string? PoSubmittedByUserId { get; set; }

    // Hardcopy Evidence Tracking Fields
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

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Hardcopy Submitted At")]
    public DateTime? HardcopySubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Hardcopy Submitted By")]
    public string? HardcopySubmittedByUserId { get; set; }

    // Rejection Tracking Fields
    [MaxLength(1000)]
    [DisplayName("Rejection Note")]
    public string? RejectionNote { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Rejected At")]
    public DateTime? RejectedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Rejected By")]
    public string? RejectedByUserId { get; set; }

    // ===== Revision Tracking Fields =====

    /// <summary>
    /// Selected symptoms during rejection (Flags enum)
    /// </summary>
    [DisplayName("Rejection Symptoms")]
    public RejectionSymptom? RejectionSymptoms { get; set; }

    /// <summary>
    /// Symptoms that still need to be addressed (for sequential revision)
    /// </summary>
    [DisplayName("Pending Revision Symptoms")]
    public RejectionSymptom? PendingRevisionSymptoms { get; set; }

    /// <summary>
    /// Status before rejection (to restore after revision if needed)
    /// </summary>
    [DisplayName("Status Before Rejection")]
    public ProcurementStatus? StatusBeforeRejection { get; set; }

    /// <summary>
    /// Number of times this procurement has been revised
    /// </summary>
    [DisplayName("Revision Count")]
    public int RevisionCount { get; set; } = 0;

    /// <summary>
    /// When the procurement was last resubmitted after revision
    /// </summary>
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Resubmitted At")]
    public DateTime? ResubmittedAt { get; set; }

    /// <summary>
    /// User who resubmitted after revision
    /// </summary>
    [MaxLength(450)]
    [DisplayName("Resubmitted By")]
    public string? ResubmittedByUserId { get; set; }

    // LDP Document Fields (Rekapan Dokumen Procurement yang sudah ditanda tangan basah)
    [MaxLength(255)]
    [DisplayName("LDP File Name")]
    public string? LdpFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("LDP File Path")]
    public string? LdpFileObjectKey { get; set; }

    [MaxLength(100)]
    [DisplayName("LDP File Content Type")]
    public string? LdpFileContentType { get; set; }

    [DisplayName("LDP File Size")]
    public long? LdpFileSize { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("LDP Uploaded At")]
    public DateTime? LdpUploadedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("LDP Uploaded By")]
    public string? LdpUploadedByUserId { get; set; }

    // Approval QR Code Token Fields
    [MaxLength(100)]
    [DisplayName("Approval Token")]
    public string? ApprovalToken { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Approval Token Generated At")]
    public DateTime? ApprovalTokenGeneratedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Approval Sent By")]
    public string? ApprovalSentByUserId { get; set; }

    // ===== End Procurement-Level Tracking Fields =====

    // ===== Approval Timeline Tracking Fields (for LDP reporting) =====

    // Manager Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Manager Approval Start")]
    public DateTime? ManagerApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Manager Approval End")]
    public DateTime? ManagerApprovalEndAt { get; set; }

    // VP Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("VP Approval Start")]
    public DateTime? VpApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("VP Approval End")]
    public DateTime? VpApprovalEndAt { get; set; }

    // Operation Director Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Op Director Approval Start")]
    public DateTime? OpDirApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Op Director Approval End")]
    public DateTime? OpDirApprovalEndAt { get; set; }

    // President Director Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("President Director Approval Start")]
    public DateTime? PresDirApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("President Director Approval End")]
    public DateTime? PresDirApprovalEndAt { get; set; }

    // ===== End Approval Timeline Tracking Fields =====

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
