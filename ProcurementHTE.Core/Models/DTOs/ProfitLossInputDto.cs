namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossInputDto
    {
        public string WorkOrderId { get; set; } = null!;
        public decimal TarifAwal { get; set; }
        public decimal TarifAdd { get; set; }
        public int KmPer25 { get; set; }

        public List<string> SelectedVendorIds { get; set; } = [];
        public List<VendorOffersDto> Vendors { get; set; } = [];
    }
}
