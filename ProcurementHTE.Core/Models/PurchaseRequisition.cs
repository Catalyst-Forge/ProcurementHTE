using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("PurchaseRequisitions")]
public class PurchaseRequisition : BaseEntity
{
    [Key]
    public string PrId { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(100)]
    [DisplayName("PR Number")]
    public string PrNumber { get; set; } = null!;

    [Required]
    [DisplayName("Request Date")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime RequestDate { get; set; }

    [MaxLength(1000)]
    [DisplayName("Description")]
    public string? Description { get; set; }

    [MaxLength(255)]
    [DisplayName("Document File Name")]
    public string? DocumentFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("Document File Path")]
    public string? DocumentFilePath { get; set; }

    [MaxLength(100)]
    [DisplayName("Document Content Type")]
    public string? DocumentContentType { get; set; }

    [DisplayName("Document File Size")]
    public long? DocumentFileSize { get; set; }

    [Required]
    [MaxLength(450)]
    [DisplayName("Created By")]
    public string CreatedByUserId { get; set; } = null!;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DisplayName("Updated At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public ICollection<Procurement> Procurements { get; set; } = [];
}
