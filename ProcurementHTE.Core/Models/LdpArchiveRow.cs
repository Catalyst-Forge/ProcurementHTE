using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

/// <summary>
/// One completed job from the LDP spreadsheet, or one an Admin added by hand. It has no
/// workflow and stays out of dashboards. The main columns are typed for search and sort,
/// and ValuesJson keeps every spreadsheet column for the detail view.
/// </summary>
public class LdpArchiveRow : BaseEntity
{
    [Key]
    [StringLength(450)]
    public string LdpArchiveRowId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(80)]
    public string ImportBatch { get; set; } = default!;

    public int SourceRow { get; set; }

    /// <summary>
    /// Identifies an imported row across imports: PR number, job name, start date, and its
    /// place among rows that share those three. Null for rows an Admin added.
    /// </summary>
    [StringLength(450)]
    public string? SourceKey { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Completed";

    [StringLength(100)] public string? Wo { get; set; }
    [StringLength(100)] public string? NoSpk { get; set; }
    [StringLength(300)] public string? VendorName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [StringLength(1000)] public string? JobName { get; set; }
    [StringLength(100)] public string? JobType { get; set; }
    [StringLength(100)] public string? ContractType { get; set; }
    [StringLength(300)] public string? LtcName { get; set; }
    [StringLength(20)] public string? YearJob { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal? NilaiPnl { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal? NilaiAccrual { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal? NilaiRealisasi { get; set; }
    [StringLength(100)] public string? NoSpmp { get; set; }
    [StringLength(100)] public string? NoPr { get; set; }
    public DateTime? PrCreatedAt { get; set; }
    [StringLength(100)] public string? NoPo { get; set; }
    public DateTime? PoDate { get; set; }
    [StringLength(100)] public string? NoInvoice { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal? InvoiceValue { get; set; }
    [StringLength(100)] public string? NoSa { get; set; }
    [StringLength(100)] public string? NoSp3 { get; set; }
    [StringLength(200)] public string? PicPr { get; set; }
    [StringLength(200)] public string? PicOps { get; set; }
    [StringLength(1000)] public string? Keterangan { get; set; }

    [Required]
    public string ValuesJson { get; set; } = "[]";

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [StringLength(450)]
    public string? UpdatedByUserId { get; set; }
}
