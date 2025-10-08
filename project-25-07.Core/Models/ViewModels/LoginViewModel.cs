using System.ComponentModel.DataAnnotations;

namespace project_25_07.Core.Models.ViewModels {
  public class LoginViewModel {
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password wajib diisi")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
  }
}
