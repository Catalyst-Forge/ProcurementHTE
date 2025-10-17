namespace ProcurementHTE.Core.Models.DTOs
{
    public class VendorOfferRowDto
    {
        public int RowIndex { get; set; }
        public string? VendorId { get; set; }
        public int OfferNumber { get; set; }
        public decimal? OfferPrice { get; set; }
        public string? ItemName { get; set; }
        public int Trip { get; set; }
        public string? Unit { get; set; }
    }
}
