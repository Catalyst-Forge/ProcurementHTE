using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLossItem {
        [Key]
        public string ProfitLossItemId { get; set; } = Guid.NewGuid().ToString();

        // Angka per-item
        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAwal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAdd { get; set; }

        [Range(0, 10000)]
        public int KmPer25 { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal OperatorCost { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal Revenue { get; set; }

        // Foreign Key
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string WoOfferId { get; set; } = null!;

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;

        [ForeignKey("WoOfferId")]
        public WoOffer WoOffer { get; set; } = default!;
    }

}
