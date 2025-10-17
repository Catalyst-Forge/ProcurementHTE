using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models.DTOs {
    public class ProfitLossViewDto {
        public string ProfitLossId { get; set; } = null!;

        [DisplayName("WO No.")]
        public string WoNum { get; set; } = null!;

        [DisplayName("Vendor Name")]
        public string VendorName { get; set; } = null!;

        [DisplayName("Revenue")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Revenue { get; set; }

        [DisplayName("Cost Operator")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CostOperator { get; set; }

        [DisplayName("Profit")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Profit { get; set; }

        [DisplayName("Profit Percentage")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ProfitPercentage { get; set; }

        [DisplayName("Best Offer Price")]
        [Column(TypeName = "decimal(18, 2")]
        public decimal? BestOfferPrice { get; set; }

        [DisplayName("Adjustment Rate")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AdjustmentRate { get; set; }

        [DisplayName("Adjusted Profit")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AdjustedProfit { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
