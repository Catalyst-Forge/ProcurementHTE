using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.Account
{
    public class ChangePasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password saat ini")]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password baru")]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi password baru")]
        [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak sama.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
