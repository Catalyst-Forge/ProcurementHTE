using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.ViewModels
{
    public class LoginWithRecoveryCodeViewModel
    {
        [Required]
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; } = null!;

        public string? ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }
}
