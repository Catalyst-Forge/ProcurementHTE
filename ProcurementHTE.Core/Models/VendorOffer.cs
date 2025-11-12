using System.ComponentModel;
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

        [MaxLength(128)]
        public string? NoLetter { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string WoOfferId { get; set; } = null!;
        public string VendorId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WorkOrder WorkOrder { get; set; } = default!;

        [ForeignKey("WoOfferId")]
        public WoOffer WoOffer { get; set; } = default!;

        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; } = default!;
    }
}
