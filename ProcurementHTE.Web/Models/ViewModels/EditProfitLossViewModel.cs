namespace ProcurementHTE.Web.Models.ViewModels {
    public class EditProfitLossViewModel {
        public string ProfitLossId { get; set; } = null!;
        public string WorkOrderId { get; set; } = null!;
        public string WoNum { get; set; } = null!;
        public string VendorName { get; set; } = null!;
        public decimal? Revenue { get; set; }
        public decimal? CostOperator { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitPercentage { get; set; }
        public decimal? Penawaran1Price { get; set; }
        public decimal? Penawaran2Price { get; set; }
        public decimal? Penawaran3Price { get; set; }
        public decimal? BestOfferPrice { get; set; }
        public decimal? AdjustmentRate { get; set; }
        public decimal? AdjustedProfit { get; set; }
    }
}
