using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class WoTypes {
    [Key]
    public int WoTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TypeName { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; }
  }
}
