using System.ComponentModel.DataAnnotations;

namespace project_25_07.Core.Models {
  public class ReasonRejected {
    [Key]
    public int ReasonId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ReasonName { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
