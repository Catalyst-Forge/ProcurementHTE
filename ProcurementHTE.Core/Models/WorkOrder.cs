using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProcurementHTE.Core.Models
{
    public enum ProcurementType
    {
        Barang = 1,
        Jasa = 2,
    }

    public class WorkOrder
    {
        [Key]
        public string WorkOrderId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [DisplayName("WO No.")]
        public string? WoNum { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Note")]
        [MaxLength(1000)]
        public string? Note { get; set; }

        [DisplayName("Procurement Type")]
        public ProcurementType ProcurementType { get; set; }

        public string? WoLetter { get; set; }

        [DisplayName("Date Letter")]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? DateLetter { get; set; }

        [DisplayName("From")]
        public string? FromLocation { get; set; }

        [DisplayName("To")]
        public string? Destination { get; set; }

        public string? WorkOrderLetter { get; set; }

        public string? WBS { get; set; }

        [DisplayName("GL Account")]
        public string? GlAccount { get; set; }

        [DisplayName("Date Required")]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? DateRequired { get; set; }

        public string? XS1 { get; set; }

        public string? XS2 { get; set; }

        public string? XS3 { get; set; }

        public string? XS4 { get; set; }

        public string? FileWorkOrder { get; set; }

        public string? Requester { get; set; }

        public string? Approved { get; set; }

        [DisplayName("Created At")]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public string? WoTypeId { get; set; }
        public int? StatusId { get; set; }
        public string? VendorId { get; set; }
        public string? UserId { get; set; }

        [ForeignKey("WoTypeId")]
        [DisplayName("Type")]
        [JsonIgnore]
        public WoTypes? WoType { get; set; }

        [ForeignKey("StatusId")]
        [DisplayName("Status")]
        [JsonIgnore]
        public Status? Status { get; set; }

        [ForeignKey("VendorId")]
        [JsonIgnore]
        public Vendor? Vendor { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }

        public ICollection<WoDocuments>? WoDocuments { get; set; } = [];
        public ICollection<WoDetail>? WoDetails { get; set; } = [];
        public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    }
}
