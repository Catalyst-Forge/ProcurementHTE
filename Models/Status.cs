using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class Status {
    [Key]
    public int StatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StatusName { get; set; }

    public virtual ICollection<WorkOrder> WorkOrders { get; set; }
  }
}
