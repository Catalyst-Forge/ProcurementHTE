using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.Account
{
    public class UpdateProfileInputModel
    {
        [Required]
        [Display(Name = "Nama depan")]
        [StringLength(100)]
        public string FirstName { get; set; } = null!;

        [Display(Name = "Nama belakang")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Jabatan / Posisi")]
        [StringLength(200)]
        public string? JobTitle { get; set; }

        [Required]
        [Display(Name = "Username")]
        [StringLength(30, MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Phone]
        [Display(Name = "Nomor HP")]
        public string? PhoneNumber { get; set; }
    }
}
