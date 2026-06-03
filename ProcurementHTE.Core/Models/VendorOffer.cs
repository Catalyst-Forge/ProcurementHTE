using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class VendorOffer : BaseEntity
    {
        [Key]
        public string VendorOfferId { get; set; } = Guid.NewGuid().ToString();

        [Display(Name = "Round")]
        public int Round { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Harga Penawaran")]
        public decimal Price { get; set; }

        [MaxLength(255)]
        [Display(Name = "Nomor Surat")]
        public string? NoLetter { get; set; }

        [Required]
        [Display(Name = "Jumlah Item")]
        public int QuantityItem { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Quantity of Unit")]
        public decimal QuantityOfUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string UnitTypeId { get; set; } = null!;

        [ForeignKey("ProcurementId")]
        public Procurement Procurement { get; set; } = default!;

        [ForeignKey("ProcOfferId")]
        public ProcOffer ProcOffer { get; set; } = default!;

        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; } = default!;

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;

        [ForeignKey("UnitTypeId")]
        public UnitType UnitType { get; set; } = default!;
    }
}
