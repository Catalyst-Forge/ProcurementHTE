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
        public decimal TarifAwal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TarifAdd { get; set; }

        public int KmPer25 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal OperatorCost { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Revenue { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SelectedVendorFinalOffer { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Profit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ProfitPercent { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string? SelectedVendorId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WorkOrder WorkOrder { get; set; } = default!;

        [ForeignKey("SelectedVendorId")]
        public Vendor SelectedVendor { get; set; } = default!;
    }
}
