using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class VendorOffer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [DisplayName("Item Name")]
        public string? ItemName { get; set; }

        [DisplayName("Quantity")]
        public int? Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalPrice { get; set; }

        public DateTime? OfferDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Foreign Key
        public string WoNum { get; set; } = null!;

        [ForeignKey("WoNum")]
        public VendorWorkOrder VendorWorkOrder { get; set; } = default!;
    }
}
