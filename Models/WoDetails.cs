using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class WoDetails {
    [Key]
    public int WoDetailId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ItemName { get; set; }

    // Foreign Key
    public int WorkOrderId { get; set; }

    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder WorkOrder { get; set; }
  }
}
