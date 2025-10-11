using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class Vendor
    {
        [Key]
        public string VendorId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string VendorCode { get; set; } = null!;

        [Required]
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

        [DisplayName("Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [DisplayName("Contact Person")]
        public string ContactPerson { get; set; } = null!;

        [Required]
        [DisplayName("Contact Position")]
        public string ContactPosition { get; set; } = null!;

        [Required]
        [DisplayName("Status")]
        public string Status { get; set; } = null!;

        [DisplayName("Comment")]
        [MaxLength(200)]
        public string? Comment { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<VendorWorkOrder> VendorWorkOrders { get; set; } = new List<VendorWorkOrder>();
    }
}
