using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class Vendor {
    [Key]
    public int VendorId { get; set; }

    [Required]
    [MaxLength(255)]
    public string VendorName { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required]
    public string Documents { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
  }
}
