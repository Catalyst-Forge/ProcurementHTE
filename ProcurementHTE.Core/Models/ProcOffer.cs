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

    /// <summary>
    /// Unit of Items - satuan fisik item (free text, e.g., "Pcs", "Unit", "Set")
    /// </summary>
    [Required]
    public string Unit { get; set; } = null!;

    /// <summary>
    /// Unit of Revenue - satuan untuk perhitungan revenue (UnitType code: TRIP, HARI, JAM, LSP, KALI)
    /// Nullable karena diisi saat create/edit PNL
    /// </summary>
    public string? UnitRevenue { get; set; }

    [Required]
    public string ProcurementId { get; set; } = null!;

    [ForeignKey(nameof(ProcurementId))]
    public Procurement Procurement { get; set; } = default!;
}
