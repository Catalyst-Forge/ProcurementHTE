using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.ViewModels {
  public class RegisterViewModel {
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format Email tidak valid")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Username wajib diisi")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username minimal 3 karakter dan maksimal 100 karakter")]
    [DisplayName("Username")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Password wajib diisi")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok")]
    [DisplayName("Confirmation Password")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Nama depan wajib diisi")]
    [DisplayName("Firstname")]
    public string FirstName { get; set; } = null!;

    [DisplayName("Lastname")]
    public string LastName { get; set; } = string.Empty;

    [DisplayName("Select Role")]
    public string SelectedRole { get; set; } = "User";
  }
}
