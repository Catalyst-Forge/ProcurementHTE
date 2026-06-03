using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProfitLossInputViewModel
    {
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        public string? JobTypeName { get; set; }

        [DisplayName("Accrual Amount")]
        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? AccrualAmount { get; set; }

        [DisplayName("Realization Amount")]
        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? RealizationAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? Distance { get; set; }

        public DateTime? TglMulaiSewa { get; set; }

        public DateTime? TglMulaiMoving { get; set; }

        [DisplayName("Daftar Item P&L")]
        [MinLength(1)]
        public List<ItemTariffInputVm> Items { get; set; } = [];

        [DisplayName("Penawaran Vendor per Item")]
        public List<VendorItemOfferInputVm> Vendors { get; set; } = [];

        public IEnumerable<VendorChoiceViewModel> VendorChoices { get; set; } = [];

        [DisplayName("Vendor yang disertakan")]
        [Required(ErrorMessage = "Pilih minimal 1 vendor")]
        [MinLength(1, ErrorMessage = "Pilih minimal 1 vendor")]
        public List<string> SelectedVendorIds { get; set; } = [];

        public List<ProcOfferLiteVm> OfferItems { get; set; } = [];
    }

    public class ProcOfferLiteVm
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [Required]
        public string ItemPenawaran { get; set; } = null!;

        [Required]
        public decimal Quantity { get; set; }

        public string? Unit { get; set; }

        public string? UnitRevenue { get; set; }
    }

    public class ItemTariffInputVm
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        /// <summary>
        /// For PENGANGKUTAN: UnitQty (number of trips)
        /// For SEWA_UNIT/MOVING: Quantity/Durasi (duration/count)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Qty Items from ProcOffer (jumlah unit fisik)
        /// </summary>
        public int QtyItems { get; set; }

        public string? UnitItems { get; set; }

        public string? UnitRevenue { get; set; }

        [DisplayName("400 KM Tariff")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? TarifAwal { get; set; }

        [DisplayName("Additional Tariff")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? TarifAdd { get; set; }

        [DisplayName("KM per 25 km")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? KmPer25 { get; set; }

        [DisplayName("Biaya Operator (Item)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? OperatorCost { get; set; }
    }

    public class VendorItemOfferInputVm
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        public List<string> Letters { get; set; } = [];

        public List<string> LetterDocIds { get; set; } = [];

        public List<bool> LetterDeletes { get; set; } = [];

        public List<IFormFile?> LetterFiles { get; set; } = [];

        [MinLength(1)]
        public List<VendorOfferPerItemInputVm> Items { get; set; } = [];
    }

    public class VendorOfferPerItemInputVm
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        public int Round { get; set; }

        [MinLength(0)]
        public List<decimal> Prices { get; set; } = [];

        public int Quantity { get; set; }

        public decimal Trip { get; set; }

        /// <summary>
        /// Indicates whether this item is included in the vendor offer.
        /// Items with IsIncluded = false are excluded from the offer.
        /// </summary>
        public bool IsIncluded { get; set; } = true;
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
        public string ProcurementId { get; set; } = null!;

        public string? ProcNum { get; set; }

        public decimal TotalOperatorCost { get; set; }
        public decimal TotalRevenue { get; set; }

        [DisplayName("Accrual Amount")]
        public decimal? AccrualAmount { get; set; }

        [DisplayName("Realization Amount")]
        public decimal? RealizationAmount { get; set; }

        public decimal? Distance { get; set; }

        [Required, StringLength(450)]
        public string SelectedVendorId { get; set; } = null!;
        public string? SelectedVendorName { get; set; }

        public decimal SelectedFinalOffer { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<(
            string ProcOfferId,
            string ItemName,
            int UnitQty,
            decimal BasePrice,
            decimal? TarifAdd,
            decimal? KmPer25,
            decimal? OperatorCost,
            decimal Revenue,
            int? Quantity,
            string? UnitRevenue,
            string? UnitItems
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
