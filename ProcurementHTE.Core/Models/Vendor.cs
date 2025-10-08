using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models {
  public class Vendor {
    [Key]
    public string VendorId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(255)]
    [DisplayName("Vendor Name")]
    public string VendorName { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [DisplayName("Price")]
    public decimal Price { get; set; }

    [DisplayName("Documents")]
    public string Documents { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Key
    [DisplayName("User")]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
  }
}
