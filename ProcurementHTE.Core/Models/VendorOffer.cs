using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class VendorOffer
    {
        [Key]
        public string VendorOfferId { get; set; } = Guid.NewGuid().ToString();

        [DisplayName("Item Name")]
        public string? ItemName { get; set; }

        [DisplayName("Trip")]
        public int? Trip { get; set; }

        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [DisplayName("Offer Number")]
        public int OfferNumber { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? OfferPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? OfferDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string VendorId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WorkOrder WorkOrder { get; set; } = default!;

        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; } = default!;
    }
}
