using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLoss
    {
        [Key]
        public string ProfitLossId { get; set; } = Guid.NewGuid().ToString();

        // Document-level
        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
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
        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Required, StringLength(450)]
        public string? SelectedVendorId { get; set; } = null!;

        [ForeignKey("ProcurementId")]
        public Procurement Procurement { get; set; } = default!;

        [ForeignKey("SelectedVendorId")]
        public Vendor SelectedVendor { get; set; } = default!;

        public ICollection<ProfitLossItem> Items { get; set; } = [];
    }
}
