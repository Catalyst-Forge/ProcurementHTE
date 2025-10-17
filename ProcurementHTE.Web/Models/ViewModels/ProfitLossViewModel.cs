namespace ProcurementHTE.Web.Models.ViewModels {
    public class ProfitLossViewModel {
        public string ProfitLossId { get; set; } = null!;
        public string WoNum { get; set; } = null!;
        public string VendorName { get; set; } = null!;
        public decimal? Revenue { get; set; }
        public decimal? CostOperator { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitPercentage { get; set; }
        public decimal? BestOfferPrice { get; set; }
        public decimal? AdjustmentRate { get; set; }
        public decimal? AdjustedProfit { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
