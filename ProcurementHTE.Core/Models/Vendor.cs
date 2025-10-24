using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class Vendor
    {
        [Key]
        public string VendorId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string VendorCode { get; set; } = null!;

        [Required]
        [DisplayName("Vendor Name")]
        public string VendorName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        [DisplayName("NPWP")]
        public string NPWP { get; set; } = null!;

        [Required]
        [DisplayName("Address")]
        [MaxLength(200)]
        public string Address { get; set; } = null!;

        [Required]
        [DisplayName("City")]
        public string City { get; set; } = null!;

        [Required]
        [DisplayName("Province")]
        public string Province { get; set; } = null!;

        [Required]
        [DisplayName("PostalCode")]
        public int PostalCode { get; set; }

        [Required]
        [DisplayName("Email Vendor")]
        public string Email { get; set; } = null!;

        [DisplayName("Comment")]
        [MaxLength(200)]
        public string? Comment { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    }
}
