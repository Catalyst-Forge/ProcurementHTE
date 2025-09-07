using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class ReasonRejected {
    [Key]
    public int ReasonId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ReasonName { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
