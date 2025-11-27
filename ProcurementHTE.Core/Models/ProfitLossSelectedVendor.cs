using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLossSelectedVendor
    {
        public string ProfitLossSelectedVendorId { get; set; } = Guid.NewGuid().ToString();
        public string ProcurementId { get; set; } = null!;
        public string VendorId { get; set; } = null!;
        public string? ProfitLossId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProcurementId")]
        public Procurement Procurement { get; set; } = default!;

        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; } = default!;

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;
    }
}
