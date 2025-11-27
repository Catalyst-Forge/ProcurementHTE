using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class ProfitLossItem
    {
        [Key]
        public string ProfitLossItemId { get; set; } = Guid.NewGuid().ToString();

        public int Quantity { get; set; }

        // Angka per-item
        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAwal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal TarifAdd { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal KmPer25 { get; set; }

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
        public string ProcOfferId { get; set; } = null!;

        [ForeignKey("ProfitLossId")]
        public ProfitLoss ProfitLoss { get; set; } = default!;

        [ForeignKey("ProcOfferId")]
        public ProcOffer ProcOffer { get; set; } = default!;
    }
}
