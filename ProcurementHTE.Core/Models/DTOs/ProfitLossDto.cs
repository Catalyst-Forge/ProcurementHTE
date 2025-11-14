using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossItemInputDto
    {
        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAwal { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAdd { get; set; }

        [Range(0, 1000)]
        public int KmPer25 { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal OperatorCost { get; set; }
    }

    public class VendorOfferPerItemDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [MinLength(0)]
        public List<decimal> Prices { get; set; } = [];

        [MinLength(0)]
        public List<string> Letters { get; set; } = [];
    }

    public class ProfitLossInputDto
    {
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        public List<string> SelectedVendorIds { get; set; } = [];

        public List<VendorItemOffersDto> Vendors { get; set; } = [];
    }

    public class VendorItemOffersDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        [MinLength(1)]
        public List<VendorOfferPerItemDto> Items { get; set; } = [];
    }

    public class ProfitLossEditDto
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        [StringLength(450)]
        public string? SelectedVendorId { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
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

        [Required, StringLength(450)]
        public string SelectedVendorId { get; set; } = null!;
        public string? SelectedVendorName { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal SelectedFinalOffer { get; set; }

        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }

        public List<(
            string ProcOfferId,
            string ItemName,
            decimal TarifAwal,
            decimal TarifAdd,
            int KmPer25,
            decimal OperatorCost,
            decimal Revenue
        )> Items { get; set; } = [];

        public List<string> SelectedVendorNames { get; set; } = [];
        public List<VendorComparisonDto> VendorComparisons { get; set; } = [];
    }
}
