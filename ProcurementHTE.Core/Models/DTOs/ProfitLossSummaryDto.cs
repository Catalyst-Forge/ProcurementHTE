namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossSummaryDto
    {
        public string ProfitLossId { get; set; } = null!;
        public string WorkOrderId { get; set; } = null!;
        public decimal TarifAwal { get; set; }
        public decimal TarifAdd { get; set; }
        public int KmPer25 { get; set; }
        public decimal OperatorCost { get; set; }
        public decimal Revenue { get; set; }
        public string SelectedVendorId { get; set; } = null!;
        public string? SelectedVendorName { get; set; }
        public decimal SelectedFinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public List<string> SelectedVendorNames { get; set; } = [];
        public List<VendorComparisonDto> VendorComparisons { get; set; } = [];
    }
}
