namespace ProcurementHTE.Core.Models.DTOs
{
    public class VendorComparisonDto
    {
        public string VendorName { get; set; } = null!;
        public decimal FinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public bool IsSelected { get; set; }
    }
}
