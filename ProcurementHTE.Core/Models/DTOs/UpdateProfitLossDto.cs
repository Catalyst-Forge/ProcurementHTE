using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class UpdateProfitLossDto
    {
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CostOperator { get; set; }

        [Column(TypeName = "decimal(18, 2")]
        public decimal? AdjustmentRate { get; set; }
    }
}
