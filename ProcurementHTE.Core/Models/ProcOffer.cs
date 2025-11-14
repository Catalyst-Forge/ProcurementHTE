using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class ProcOffer
{
    [Key]
    public string ProcOfferId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string ItemPenawaran { get; set; } = null!;
    [Required]
    public int Qty { get; set; }
    [Required]
    public string Unit { get; set; } = null!;

    [Required]
    public string ProcurementId { get; set; } = null!;

    [ForeignKey(nameof(ProcurementId))]
    public Procurement Procurement { get; set; } = default!;
}
