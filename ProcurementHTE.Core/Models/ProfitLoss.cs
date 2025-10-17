using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLoss
    {
        [Key]
        public string ProfitLossId { get; set; } = Guid.NewGuid().ToString();

        // Tagihan
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Revenue { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CostOperator { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Profit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ProfitPercentage { get; set; }

        // Penawaran Mitra
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? HargaPenawaran1 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? HargaPenawaran2 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? HargaPenawaran3 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? BestOfferPrice { get; set; }

        // Adjustment
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AdjustmentRate { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AdjustedProfit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PotentialNewProfit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ProfitRevenue { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string SelectedVendorOfferId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WorkOrder WorkOrder { get; set; } = default!;

        [ForeignKey("SelectedVendorOfferId")]
        public VendorOffer SelectedVendorOffer { get; set; } = default!;
    }
}
