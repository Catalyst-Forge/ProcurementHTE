namespace ProcurementHTE.Core.Models
{
    public class ProfitLossSelectedVendor
    {
        public string ProfitLossSelectedVendorId { get; set; } = Guid.NewGuid().ToString();
        public string ProcurementId { get; set; } = null!;
        public string VendorId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
