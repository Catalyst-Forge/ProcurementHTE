using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class VendorOffer
    {
        [Key]
        public string VendorOfferId { get; set; } = Guid.NewGuid().ToString();

        public int Round { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(128)]
        public string NoLetter { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int Trip { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public string ProcurementId { get; set; } = null!;
        public string ProcOfferId { get; set; } = null!;
        public string VendorId { get; set; } = null!;
        public string ProfitLossId { get; set; } = null!;

        [ForeignKey("ProcurementId")]
        public Procurement Procurement { get; set; } = default!;

        [ForeignKey("ProcOfferId")]
        public ProcOffer ProcOffer { get; set; } = default!;

        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; } = default!;

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;
    }
}
