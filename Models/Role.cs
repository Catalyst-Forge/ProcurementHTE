using System.ComponentModel.DataAnnotations;

namespace project_25_07.Models {
  public class Role {
    [Key]
    public int RoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<UserRole> UserRoles { get; set; }
  }
}
