using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class Tender {
    [Key]
    public int TenderId { get; set; }

    [Required]
    [MaxLength(255)]
    public string TenderName { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }

    public string? Information { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
