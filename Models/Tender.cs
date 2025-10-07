using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class Tender {
    [Key]
    public int TenderId { get; set; }

    [Required]
    [MaxLength(255)]
    [DisplayName("Tender Name")]
    public string TenderName { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    [DisplayName("Price")]
    public decimal Price { get; set; }

    [DisplayName("Information")]
    public string? Information { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
