using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDetail
    {
        [Key]
        public string WoDetailId { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(255)]
        public string? ItemName { get; set; }

        [DisplayName("Quantity")]
        public int? Quantity { get; set; }

        [DisplayName("Unit")]
        public string? Unit { get; set; }

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder WorkOrder { get; set; } = default!;
    }
}
