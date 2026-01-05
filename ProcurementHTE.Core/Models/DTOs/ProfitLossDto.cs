using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossItemInputDto
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        /// <summary>
        /// For PENGANGKUTAN: This is UnitQty (number of trips)
        /// For SEWA_UNIT/MOVING: This is Quantity/Durasi (duration/count)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Qty Items from ProcOffer (jumlah unit fisik)
        /// Used by SEWA_UNIT and MOVING to store actual item quantity
        /// </summary>
        public int QtyItems { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number."
        )]
        public decimal TarifAwal { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number."
        )]
        public decimal TarifAdd { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number."
        )]
        public decimal KmPer25 { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number."
        )]
        public decimal OperatorCost { get; set; }

        /// <summary>
        /// Unit Revenue - satuan untuk perhitungan revenue (TRIP, HARI, JAM, LSP, KALI)
        /// Will be saved to ProcOffer.UnitRevenue for persistence
        /// </summary>
        public string? UnitRevenue { get; set; }
    }

    public class VendorItemOffersDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        [MinLength(0)]
        public List<string> Letters { get; set; } = [];

        [MinLength(0)]
        public List<string?> LetterDocIds { get; set; } = [];

        [MinLength(1)]
        public List<VendorOfferPerItemDto> Items { get; set; } = [];
    }

    public class VendorOfferPerItemDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        public int Round { get; set; }

        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

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

    public class ProfitLossInputDto
    {
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? AccrualAmount { get; set; }

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

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        public List<string> SelectedVendorIds { get; set; } = [];

        public List<VendorItemOffersDto> Vendors { get; set; } = [];
    }

    public class ProfitLossEditDto
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? AccrualAmount { get; set; }

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

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        [StringLength(450)]
        public string? SelectedVendorId { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal SelectedVendorFinalOffer { get; set; }

        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }

        public byte[]? RowVersion { get; set; }

        public List<string> SelectedVendorIds { get; set; } = [];
        public List<VendorItemOffersDto> Vendors { get; set; } = [];
    }

    public class ProfitLossUpdateDto
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? AccrualAmount { get; set; }

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

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        public List<string> SelectedVendorIds { get; set; } = [];

        public List<VendorItemOffersDto> Vendors { get; set; } = [];

        public byte[]? RowVersion { get; set; }
    }

    public class ProfitLossSummaryDto
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        public decimal TotalOperatorCost { get; set; }
        public decimal TotalRevenue { get; set; }

        public decimal? AccrualAmount { get; set; }
        public decimal? RealizationAmount { get; set; }
        public decimal? Distance { get; set; }

        [Required, StringLength(450)]
        public string SelectedVendorId { get; set; } = null!;
        public string? SelectedVendorName { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
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
        public List<VendorComparisonDto> VendorComparisons { get; set; } = [];
    }

    public class ProfitLossNegotiationTablesDto
    {
        public string ProfitLossId { get; set; } = null!;
        public string ProcurementId { get; set; } = null!;
        public List<ProfitLossVendorNegotiationTableDto> Vendors { get; set; } = [];
    }

    public class ProfitLossVendorNegotiationTableDto
    {
        public string VendorId { get; set; } = null!;
        public string VendorName { get; set; } = null!;
        public int MaxRound { get; set; }
        public List<ProfitLossVendorRoundInfoDto> Rounds { get; set; } = [];
        public List<ProfitLossVendorItemNegotiationDto> Items { get; set; } = [];
        public decimal GrandTotal { get; set; }
        public bool IsSelectedVendor { get; set; }
    }

    public class ProfitLossVendorRoundInfoDto
    {
        public int Round { get; set; }
        public string? LetterNumber { get; set; }
    }

    public class ProfitLossVendorItemNegotiationDto
    {
        public string ProcOfferId { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public int Trip { get; set; }
        public List<decimal?> PricesPerRound { get; set; } = [];
        public decimal? FinalPrice { get; set; }
        public decimal? Total { get; set; }
    }
}
