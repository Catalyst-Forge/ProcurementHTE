namespace ProcurementHTE.Core.Models.DTOs
{
    public class VendorOffersDto
    {
        public string VendorId { get; set; } = null!;
        public List<decimal> Prices { get; set; } = [];
    }
}
