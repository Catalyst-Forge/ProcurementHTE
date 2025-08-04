using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project_25_07.Models {
  public class User {
    [Key]
    public string UserId { get; set; } = null!;

    [Required(ErrorMessage = "Email wajib diisi")]
    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [DisplayName("Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Username wajib diisi")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username harus antara 3-30 karakter")]
    [DisplayName("Username")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Password wajib diisi")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
    [DataType(DataType.Password)]
    [DisplayName("Password")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Nama depan wajib diisi")]
    [StringLength(100)]
    public string Firstname { get; set; } = null!;

    [StringLength(100)]
    public string Lastname { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string Role { get; set; } = "User";

    [NotMapped]
    public string Fullname => $"{Firstname} {Lastname}".Trim();

    [NotMapped]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok")]
    public string ConfirmPassword { get; set; } = null!;

    public virtual ICollection<UserRole> UserRoles { get; set; }
  }
}
