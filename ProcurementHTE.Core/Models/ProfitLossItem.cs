using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLossItem
    {
        [Key]
        public string ProfitLossItemId { get; set; } = Guid.NewGuid().ToString();

        // === COMMON FIELDS (All JobTypes) ===
        [Display(Name = "Nama Item")]
        [MaxLength(255)]
        public string ItemName { get; set; } = null!;

        [Display(Name = "Jumlah Unit")]
        public int UnitQty { get; set; } = 1;

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Display(Name = "Harga Dasar")]
        public decimal BasePrice { get; set; }

        // === QUANTITY/MULTIPLIER (SEWA_UNIT & MOVING only) ===
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Quantity/Durasi")]
        public decimal? Quantity { get; set; }

        // === PENGANGKUTAN SPECIFIC ===
        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Display(Name = "Tarif Tambahan")]
        public decimal? TarifAdd { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Display(Name = "Km Per 25")]
        public decimal? KmPer25 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Display(Name = "Biaya Operator")]
        public decimal? OperatorCost { get; set; }

        // === CALCULATED (All JobTypes) ===
        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        [Display(Name = "Revenue")]
        public decimal Revenue { get; set; }

        [Display(Name = "Urutan")]
        public int SortOrder { get; set; } = 0;

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [StringLength(450)]
        public string? UnitTypeId { get; set; }

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;

        [ForeignKey("ProcOfferId")]
        public ProcOffer ProcOffer { get; set; } = default!;

        [ForeignKey("UnitTypeId")]
        public UnitType? UnitType { get; set; }
    }
}
