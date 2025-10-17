using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels {
    public class CreateProfitLossViewModel {
        public string WorkOrderId { get; set; } = null!;
        public string WoNum { get; set; } = null!;
        public DateTime IssuedDate { get; set; } = DateTime.Now;
        public decimal Revenue { get; set; }
        public decimal AdjustmentRate { get; set; } = 15m;
        public decimal CostOperator { get; set; }
        public string? SelectedBestVendorId { get; set; }
        public List<SelectListItem> Vendors { get; set; } = [];
        public List<VendorOfferRowDto> VendorOffers { get; set; } = [];
    }
}
