using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class Procurement
{
    [Key]
    public string ProcurementId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    [DisplayName("No. Procurement")]
    public string ProcNum { get; set; } = null!;

    [MaxLength(100)]
    [DisplayName("No. SPK")]
    public string? SpkNumber { get; set; }

    [MaxLength(32)]
    [DisplayName("Jenis Pekerjaan")]
    public string? JobType { get; set; }

    [MaxLength(255)]
    public string? JobTypeOther { get; set; }

    [MaxLength(100)]
    public string? ContractType { get; set; }

    [MaxLength(255)]
    [DisplayName("Nama Pekerjaan")]
    public string? JobName { get; set; }

    [DisplayName("Tanggal Mulai")]
    public DateTime? StartDate { get; set; }

    [DisplayName("Tanggal Selesai")]
    public DateTime? EndDate { get; set; }

    [MaxLength(16)]
    public string? ProjectRegion { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DistanceKm { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AccrualAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RealizationAmount { get; set; }

    public DateTime? PotentialAccrualDate { get; set; }

    [MaxLength(100)]
    public string? SpmpNumber { get; set; }

    [MaxLength(100)]
    public string? MemoNumber { get; set; }

    [MaxLength(100)]
    public string? OeNumber { get; set; }

    [MaxLength(255)]
    public string? SelectedVendorName { get; set; }

    [MaxLength(100)]
    public string? VendorSphNumber { get; set; }

    [MaxLength(100)]
    public string? RaNumber { get; set; }

    [MaxLength(64)]
    public string? ProjectCode { get; set; }

    [MaxLength(255)]
    public string? LtcName { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public string? JobTypeId { get; set; }
    public int StatusId { get; set; }
    public string? UserId { get; set; }

    [MaxLength(450)]
    public string? PicOpsUserId { get; set; }

    [MaxLength(450)]
    public string? AnalystHteSignerUserId { get; set; }

    [MaxLength(450)]
    public string? AssistantManagerSignerUserId { get; set; }

    [MaxLength(450)]
    public string? ManagerSignerUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? UpdatedAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(JobTypeId))]
    public JobTypes? JobTypeConfig { get; set; }

    [ForeignKey(nameof(StatusId))]
    public Status? Status { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(PicOpsUserId))]
    public User? PicOpsUser { get; set; }

    [ForeignKey(nameof(AnalystHteSignerUserId))]
    public User? AnalystHteSignerUser { get; set; }

    [ForeignKey(nameof(AssistantManagerSignerUserId))]
    public User? AssistantManagerSignerUser { get; set; }

    [ForeignKey(nameof(ManagerSignerUserId))]
    public User? ManagerSignerUser { get; set; }

    public ICollection<ProcOffer> ProcOffers { get; set; } = [];
    public ICollection<ProcDocuments>? ProcDocuments { get; set; } = [];
    public ICollection<ProcDetail>? ProcDetails { get; set; } = [];
    public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    public ICollection<ProcDocumentApprovals> DocumentApprovals { get; set; } = [];
    public ICollection<ProfitLoss> ProfitLosses { get; set; } = [];
}
