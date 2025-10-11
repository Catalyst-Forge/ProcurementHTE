using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class VendorWorkOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal InitialOfferTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal FinalNegotiationTotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Efficiency { get; set; }

        // Foreign Keys
        public string WoNum { get; set; } = null!;

        [ForeignKey("WoNum")]
        public WorkOrder WorkOrder { get; set; } = default!;

        public string VendorCode { get; set; } = null!;

        [ForeignKey("VendorCode")]
        public Vendor Vendor { get; set; } = default!;

        public ICollection<VendorOffer> VendorOffers { get; set; } = new List<VendorOffer>();
    }
}
