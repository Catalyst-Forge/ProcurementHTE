namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossEditDto
    {
        public string ProfitLossId { get; set; } = null!;
        public string WorkOrderId { get; set; } = null!;
        public decimal TarifAwal { get; set; }
        public decimal TarifAdd { get; set; }
        public int KmPer25 { get; set; }
        public decimal OperatorCost { get; set; }
        public decimal Revenue { get; set; }
        public string? SelectedVendorId { get; set; }
        public decimal SelectedVendorFinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public byte[]? RowVersion { get; set; }

        public List<string> SelectedVendorIds { get; set; } = [];
        public List<VendorOffersDto> Vendors { get; set; } = [];
    }
}
