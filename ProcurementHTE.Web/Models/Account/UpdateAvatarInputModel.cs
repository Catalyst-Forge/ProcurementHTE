using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.Account
{
    public class UpdateAvatarInputModel
    {
        [Required(ErrorMessage = "Gambar wajib dipilih.")]
        public string ImageData { get; set; } = null!;
    }
}
