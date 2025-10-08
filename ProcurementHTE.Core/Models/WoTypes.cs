using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models {
  public class WoTypes {
    [Key]
    public int WoTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; }
  }
}
