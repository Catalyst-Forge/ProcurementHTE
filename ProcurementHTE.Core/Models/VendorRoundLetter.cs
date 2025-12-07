using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class VendorRoundLetter
{
    [Key]
    public string VendorRoundLetterId { get; set; } = Guid.NewGuid().ToString();

    [Required, StringLength(450)]
    public string ProcurementId { get; set; } = null!;

    [StringLength(450)]
    public string? ProfitLossId { get; set; }

    [Required, StringLength(450)]
    public string VendorId { get; set; } = null!;

    public int Round { get; set; }

    [StringLength(300)]
    public string? LetterNumber { get; set; }

    [Required, StringLength(450)]
    public string ProcDocumentId { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(ProcDocumentId))]
    public ProcDocuments ProcDocument { get; set; } = default!;

    [ForeignKey(nameof(VendorId))]
    public Vendor Vendor { get; set; } = default!;
}
