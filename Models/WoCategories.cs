using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class WoCategories {
    [Key]
    public int WoCategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = null!;

    public virtual ICollection<WorkOrder> WorkOrders { get; set; }
  }
}
