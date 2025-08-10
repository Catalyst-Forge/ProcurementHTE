using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class User : IdentityUser {
    [Required(ErrorMessage = "Username wajib diisi")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username harus antara 3-30 karakter")]
    [DisplayName("Username")]
    public override string? UserName { get; set; } = null!;

    [Required(ErrorMessage = "Nama depan wajib diisi")]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [NotMapped]
    public string Fullname => $"{FirstName} {LastName}".Trim();

    public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = [];
  }
}
