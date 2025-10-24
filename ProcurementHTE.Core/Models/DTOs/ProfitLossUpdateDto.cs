namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossUpdateDto
    {
        public string ProfitLossId { get; set; } = null!;
        public string WorkOrderId { get; set; } = null!;
        public decimal TarifAwal { get; set; }
        public decimal TarifAdd { get; set; }
        public int KmPer25 { get; set; }

        public List<string> SelectedVendorIds { get; set; } = [];
        public List<VendorOffersDto> Vendors { get; set; } = [];
        public byte[]? RowVersion { get; set; }
    }
}
