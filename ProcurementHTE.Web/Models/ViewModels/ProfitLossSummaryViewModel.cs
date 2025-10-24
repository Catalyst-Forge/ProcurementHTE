using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProfitLossSummaryViewModel
    {
        public string ProfitLossId { get; set; } = null!;
        public string WorkOrderId { get; set; } = null!;
        public string? WoNum { get; set; }
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

        public List<(
            string VendorName,
            decimal FinalOffer,
            decimal Profit,
            decimal ProfitPercent,
            bool IsSelected
        )> Rows { get; set; } = [];
    }
}
