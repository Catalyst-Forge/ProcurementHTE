using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs {
    public class LoginRequestDto {
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password wajib diisi")]
        public string Password { get; set; } = null!;
        public string? DeviceId { get; set; }
    }
}
