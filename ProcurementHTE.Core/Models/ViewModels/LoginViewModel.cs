using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.ViewModels {
  public class LoginViewModel {
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password wajib diisi")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
  }
}
