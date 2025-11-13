using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProfitLossInputViewModel
    {
        [Required, StringLength(450)]
        public string WorkOrderId { get; set; } = null!;

        [DisplayName("Daftar Item P&L")]
        [MinLength(1)]
        public List<ItemTariffInputVm> Items { get; set; } = [];

        [DisplayName("Penawaran Vendor per Item")]
        public List<VendorItemOfferInputVm> Vendors { get; set; } = [];

        public IEnumerable<VendorChoiceViewModel> VendorChoices { get; set; } = [];

        [DisplayName("Vendor yang disertakan")]
        public List<string> SelectedVendorIds { get; set; } = [];

        public List<WoOfferLiteVm> WoItems { get; set; } = [];
    }

    public class WoOfferLiteVm
    {
        [Required, StringLength(450)]
        public string WoOfferId { get; set; } = null!;

        [Required]
        public string ItemPenawaran { get; set; } = null!;
    }

    public class ItemTariffInputVm
    {
        [Required, StringLength(450)]
        public string WoOfferId { get; set; } = null!;

        [DisplayName("Tarif 400 km")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAwal { get; set; }

        [DisplayName("Tarif Add")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAdd { get; set; }

        [DisplayName("KM per 25 km")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal KmPer25 { get; set; }

        [DisplayName("Biaya Operator (Item)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal OperatorCost { get; set; }
    }

    public class VendorItemOfferInputVm
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        [MinLength(1)]
        public List<VendorOfferPerItemInputVm> Items { get; set; } = [];
    }

    public class VendorOfferPerItemInputVm
    {
        [Required, StringLength(450)]
        public string WoOfferId { get; set; } = null!;

        [MinLength(0)]
        public List<decimal> Prices { get; set; } = [];

        [MinLength(0)]
        public List<string> Letters { get; set; } = [];
    }

    public class VendorChoiceViewModel
    {
        [Required]
        public string Id { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;
    }

    public class ProfitLossEditViewModel : ProfitLossInputViewModel
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        public byte[]? RowVersion { get; set; }
    }

    public class ProfitLossSummaryViewModel
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string WorkOrderId { get; set; } = null!;

        public string? WoNum { get; set; }

        public decimal TotalOperatorCost { get; set; }
        public decimal TotalRevenue { get; set; }

        [Required, StringLength(450)]
        public string SelectedVendorId { get; set; } = null!;
        public string? SelectedVendorName { get; set; }

        public decimal SelectedFinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }

        public List<(
            string WoOfferId,
            string ItemName,
            decimal TarifAwal,
            decimal TarifAdd,
            decimal KmPer25,
            decimal OperatorCost,
            decimal Revenue
        )> Items { get; set; } = [];

        public List<string> SelectedVendorNames { get; set; } = [];

        public List<(
            string VendorName,
            decimal FinalOffer,
            decimal Profit,
            decimal ProfitPercent,
            bool IsSelected
        )> Rows { get; set; } = [];
    }
}
