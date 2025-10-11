using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProcurementHTE.Core.Models
{
    public class WorkOrder
    {
        [Key]
        public string WorkOrderId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string WoNum { get; set; } = null!;

        [DisplayName("Description")]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        public string? ProcurementType { get; set; }

        public string WoLetter { get; set; } = null!;

        public DateTime? DateLetter { get; set; }

        public string? FromLocation { get; set; }

        public string? Destination { get; set; }

        public string? WorkOrderLetter { get; set; }

        public string? WBS { get; set; }

        public string GlAccount { get; set; } = null!;

        public DateTime DateRequired { get; set; }

        public string XS1 { get; set; } = null!;

        public string? XS2 { get; set; }

        public string? XS3 { get; set; }

        public string? XS4 { get; set; }

        public string FileWorkOrder { get; set; } = null!;

        public string Requester { get; set; } = null!;

        public string Approved { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public int WoTypeId { get; set; }

        public int StatusId { get; set; } = 1;

        public string? VendorId { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("WoTypeId")]
        [JsonIgnore]
        public WoTypes WoType { get; set; } = default!;

        [ForeignKey("StatusId")]
        [JsonIgnore]
        public Status Status { get; set; } = default!;

        [ForeignKey("VendorId")]
        [JsonIgnore]
        public Vendor Vendor { get; set; } = default!;

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User User { get; set; } = default!;

        public ICollection<WoDocuments>? WoDocuments { get; set; } = new List<WoDocuments>();

        public ICollection<WoDetail>? WoDetails { get; set; } = new List<WoDetail>();

        public ICollection<VendorWorkOrder> VendorWorkOrders { get; set; } = new List<VendorWorkOrder>();
    }
}
