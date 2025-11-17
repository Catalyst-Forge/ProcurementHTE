using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Models.ViewModels
{
    public class LoginWith2faViewModel
    {
        [Required]
        [Display(Name = "Kode verifikasi")]
        [StringLength(8, MinimumLength = 6, ErrorMessage = "Kode harus 6-8 digit.")]
        public string TwoFactorCode { get; set; } = null!;

        [Display(Name = "Ingat perangkat ini")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public TwoFactorMethod Method { get; set; }
    }
}
