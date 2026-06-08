using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models;

public partial class Procurement : BaseEntity
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
}
