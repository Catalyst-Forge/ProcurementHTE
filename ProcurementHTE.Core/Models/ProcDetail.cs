using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class ProcDetail
{
    [Key]
    public string ProcDetailId { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(255)]
    [DisplayName("Nama Unit")]
    public string? ItemName { get; set; }

    [DisplayName("Quantity")]
    public int? Quantity { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [Required, MaxLength(32)]
    [DisplayName("Jenis Detail")]
    public string DetailKind { get; set; } = "KEBUTUHAN_UNIT";

    public string? VendorId { get; set; }

    [Required]
    public string ProcurementId { get; set; } = null!;

    [ForeignKey(nameof(ProcurementId))]
    public Procurement Procurement { get; set; } = default!;

    [ForeignKey(nameof(VendorId))]
    public Vendor? Vendor { get; set; }
}
