namespace ProcurementHTE.Core.Models.DTOs {
    public class CreateProfitLossInputDto {
        public string WorkOrderId { get; set; } = null!;
        public string SelectedVendorId { get; set; } = null!;
        public decimal CostOperator { get; set; }
        public decimal AdjustmentRate { get; set; }
        public decimal Revenue { get; set; }
    }
}
