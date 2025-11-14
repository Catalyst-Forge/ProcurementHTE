using ProcurementHTE.Core.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class Procurement
{
    [Key]
    public string ProcurementId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    [DisplayName("Procurement No.")]
    public string ProcNum { get; set; } = null!;

    [MaxLength(100)]
    [DisplayName("SPK No.")]
    public string? SpkNumber { get; set; }

    [MaxLength(255)]
    public string? JobTypeOther { get; set; }

    [Required]
    [DisplayName("Contract Type")]
    public ContractType ContractType { get; set; }

    [Required]
    [MaxLength(255)]
    [DisplayName("Job Name")]
    public string JobName { get; set; } = null!;

    [Required]
    [DisplayName("Start Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime StartDate { get; set; }

    [Required]
    [DisplayName("End Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime EndDate { get; set; }

    [Required]
    [DisplayName("Project Region")]
    public ProjectRegion ProjectRegion { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DisplayName("Accrual Amount")]
    public decimal? AccrualAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DisplayName("Realization Amount")]
    public decimal? RealizationAmount { get; set; }

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

    [Required]
    [MaxLength(450)]
    [DisplayName("PIC User")]
    public string PicOpsUserId { get; set; } = null!;

    [Required]
    [MaxLength(450)]
    [DisplayName("Analyst HTE User")]
    public string AnalystHteUserId { get; set; } = null!;

    [Required]
    [MaxLength(450)]
    [DisplayName("Assistant Manager User")]
    public string AssistantManagerUserId { get; set; } = null!;

    [Required]
    [MaxLength(450)]
    [DisplayName("Manager User")]
    public string ManagerUserId { get; set; } = null!;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? UpdatedAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? CompletedAt { get; set; }

    // Foreign Key
    public string? JobTypeId { get; set; }
    public int StatusId { get; set; }
    public string? UserId { get; set; }

    // Nav
    [ForeignKey(nameof(JobTypeId))]
    public JobTypes? JobType { get; set; }

    [ForeignKey(nameof(StatusId))]
    public Status? Status { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public ICollection<ProcOffer> ProcOffers { get; set; } = [];
    public ICollection<ProcDocuments>? ProcDocuments { get; set; } = [];
    public ICollection<ProcDetail>? ProcDetails { get; set; } = [];
    public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    public ICollection<ProcDocumentApprovals> DocumentApprovals { get; set; } = [];
    public ICollection<ProfitLoss> ProfitLosses { get; set; } = [];
}
