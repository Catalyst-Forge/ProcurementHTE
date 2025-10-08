using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models {
  public class WoDetails {
    [Key]
    public int WoDetailId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ItemName { get; set; } = null!;

    // Foreign Key
    public string WorkOrderId { get; set; } = null!;

    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; } = null!;
  }
}
