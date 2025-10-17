namespace ProcurementHTE.Web.Models.ViewModels {
    public class DeleteProfitLossViewModel {
        public string ProfitLossId { get; set; } = null!;
        public string WoNum { get; set; } = null!;
        public string VendorName { get; set; } = null!;
        public decimal? Profit { get; set; }
    }
}
