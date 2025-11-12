using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoOffer
    {
        [Key]
        public string WoOfferId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ItemPenawaran { get; set; } = null!;

        // Foreign Key
        public string WorkOrderId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder WorkOrder { get; set; } = default!;
    }
}
