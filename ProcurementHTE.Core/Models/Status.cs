using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models {
  public class Status {
    [Key]
    public int StatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StatusName { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; }
  }
}
