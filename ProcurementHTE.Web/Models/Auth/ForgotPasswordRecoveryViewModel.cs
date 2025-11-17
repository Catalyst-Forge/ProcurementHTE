using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.Auth
{
    public class ForgotPasswordRecoveryViewModel
    {
        [Required]
        [Display(Name = "Email atau Username")]
        public string Identifier { get; set; } = null!;

        [Required]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak sama.")]
        [Display(Name = "Konfirmasi Password Baru")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
