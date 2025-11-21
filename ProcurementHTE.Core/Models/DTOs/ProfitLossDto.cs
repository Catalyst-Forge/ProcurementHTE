using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossItemInputDto
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        public int Quantity { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number.")]
        public decimal TarifAwal { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number.")]
        public decimal TarifAdd { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number.")]
        public decimal KmPer25 { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative number.")]
        public decimal OperatorCost { get; set; }
    }

    public class VendorItemOffersDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

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

        [MinLength(0)]
        public List<string> Letters { get; set; } = [];

        public int Quantity { get; set; }
        public int Trip { get; set; }
    }

    public class ProfitLossInputDto
    {
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? AccrualAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? RealizationAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Distance { get; set; }

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

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? AccrualAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? RealizationAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Distance { get; set; }

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        [StringLength(450)]
        public string? SelectedVendorId { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
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

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? AccrualAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal? RealizationAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Distance { get; set; }

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

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount.")]
        public decimal SelectedFinalOffer { get; set; }

        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<(
            string ProcOfferId,
            string ItemName,
            int Quantity,
            decimal TarifAwal,
            decimal TarifAdd,
            decimal KmPer25,
            decimal OperatorCost,
            decimal Revenue
        )> Items
        { get; set; } = [];

        public List<string> SelectedVendorNames { get; set; } = [];
        public List<VendorComparisonDto> VendorComparisons { get; set; } = [];
    }

    public class ProfitLossNegotiationTablesDto
    {
        public string ProfitLossId { get; set; } = null!;
        public string ProcurementId { get; set; } = null!;
        public List<ProfitLossVendorNegotiationTableDto> Vendors { get; set; } = [];
    }

    public class ProfitLossVendorNegotiationTableDto {
        public string VendorId { get; set; } = null!;
        public string VendorName { get; set; } = null!;
        public int MaxRound { get; set; }
        public List<ProfitLossVendorRoundInfoDto> Rounds { get; set; } = [];
        public List<ProfitLossVendorItemNegotiationDto> Items { get; set; } = [];
        public decimal GrandTotal { get; set; }
        public bool IsSelectedVendor { get; set; }
    }

    public class ProfitLossVendorRoundInfoDto {
        public int Round { get; set; }
        public string? LetterNumber { get; set; }
    }

    public class ProfitLossVendorItemNegotiationDto {
        public string ProcOfferId { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public int Trip { get; set; }
        public List<decimal?> PricesPerRound { get; set; } = [];
        public decimal? FinalPrice { get; set; }
        public decimal? Total { get; set; }
    }
}
