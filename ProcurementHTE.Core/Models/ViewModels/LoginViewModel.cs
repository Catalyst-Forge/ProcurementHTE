using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "NIP atau email wajib diisi")]
        [StringLength(256)]
        [Display(Name = "NIP atau Email")]
        public string Login { get; set; } = null!;

        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Jawaban captcha wajib diisi")]
        [Display(Name = "Captcha")]
        public int? CaptchaAnswer { get; set; }

        [Display(Name = "Ingat saya")]
        public bool RememberMe { get; set; }
    }
}
