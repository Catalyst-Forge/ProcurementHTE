using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Core.Models {
  public class UserClaim {
    [Key]
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }
    public virtual User User { get; set; }

    [Required]
    [StringLength(100)]
    public string ClaimType { get; set; }

    [StringLength(200)]
    public string ClaimValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
}
