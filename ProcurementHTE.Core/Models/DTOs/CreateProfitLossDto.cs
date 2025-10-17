namespace ProcurementHTE.Core.Models.DTOs {
    public class CreateProfitLossDto {
        public string WorkOrderId { get; set; } = null!;
        public string WoNum { get; set; } = null!;
        public string SelectedVendorOfferId { get; set; } = null!;
    }
}
