using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProcurementHTE.Core.Models {
  public class WorkOrder {
    [Key]
    public string WorkOrderId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(255)]
    [DisplayName("Work Order Name")]
    public string WoName { get; set; } = null!;

    [DisplayName("Description")]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    [Required]
    [DisplayName("Work Order Type")]
    public int WoTypeId { get; set; }
    
    [Required]
    [DisplayName("Status")]
    public int StatusId { get; set; }

    [DisplayName("Tender")]
    public string TenderId { get; set; } = null!;

    [ForeignKey("WoTypeId")]
    [JsonIgnore]
    public WoTypes? WoType { get; set; }

    [ForeignKey("StatusId")]
    [JsonIgnore]
    public Status? Status { get; set; }

    [ForeignKey("TenderId")]
    [JsonIgnore]
    public Tender? Tender { get; set; }

    public ICollection<WoDetails>? ItemDetails { get; set; }
  }
}
