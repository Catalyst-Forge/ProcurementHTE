using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("WoDocuments")]
public class WoDocuments
{
    [Key]
    public string WoDocumentId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string WorkOrderId { get; set; } = default!;

    [Required]
    public string DocumentTypeId { get; set; } = default!;

    [Required, MaxLength(300)]
    public string FileName { get; set; } = default!;

    [Required, MaxLength(600)]
    public string ObjectKey { get; set; } = default!; // path objek di MinIO

    [Required, MaxLength(150)]
    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    public long Size { get; set; } = 0;

    // Uploaded / Deleted / Replaced
    [Required, MaxLength(16)]
    public string Status { get; set; } = "Uploaded";

    [MaxLength(512)]
    public string? QrText { get; set; } // payload QR

    [MaxLength(600)]
    public string? QrObjectKey { get; set; } // path PNG QR di MinIO

    [MaxLength(200)]
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // audit / ringkasan approval
    [MaxLength(450)]
    public string? CreatedByUserId { get; set; } // upload by
    public bool? IsApproved { get; set; } // null=pending
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(450)]
    public string? ApprovedByUserId { get; set; }

    // navigations
    [ForeignKey(nameof(WorkOrderId))]
    public WorkOrder WorkOrder { get; set; } = default!;

    [ForeignKey(nameof(DocumentTypeId))]
    public DocumentType DocumentType { get; set; } = default!;

    // ⬇ koleksi approval instans untuk dokumen ini
    public ICollection<WoDocumentApprovals> Approvals { get; set; } =
        new List<WoDocumentApprovals>();
}
