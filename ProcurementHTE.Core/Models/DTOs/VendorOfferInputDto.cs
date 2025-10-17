namespace ProcurementHTE.Core.Models.DTOs {
    public class VendorOfferInputDto {
        public string VendorId { get; set; } = null!;
        public string? ItemName { get; set; }
        public int? Trip { get; set; }
        public string? Unit { get; set; }
        public int OfferNumber { get; set; }
        public decimal? OfferPrice { get; set; }
    }
}
