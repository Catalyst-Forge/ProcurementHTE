using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class WorkOrder {
    [Key]
    public int WorkOrderId { get; set; }

    [Required]
    [MaxLength(255)]
    public string WoName { get; set; }

    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int WoCategoryId { get; set; }
    public int StatusId { get; set; }
    public int? TenderId { get; set; }

    [ForeignKey("WoCategoryId")]
    public virtual WoCategories WoCategory { get; set; }

    [ForeignKey("StatusId")]
    public virtual Status Status { get; set; }

    [ForeignKey("TenderId")]
    public virtual Tender? Tender { get; set; }

    public virtual ICollection<WoDetails> ItemDetails { get; set; }
  }
}
