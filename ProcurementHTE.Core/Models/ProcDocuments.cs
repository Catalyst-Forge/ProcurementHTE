using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("ProcDocuments")]
public class ProcDocuments
{
    [Key]
    public string ProcDocumentId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string ProcurementId { get; set; } = default!;

    [Required]
    public string DocumentTypeId { get; set; } = default!;

    [Required, MaxLength(300)]
    public string FileName { get; set; } = default!;

    [Required, MaxLength(600)]
    public string ObjectKey { get; set; } = default!;

    [Required, MaxLength(150)]
    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    public long Size { get; set; }

    [Required, MaxLength(16)]
    public string Status { get; set; } = "Upload";

    [MaxLength(512)]
    public string? QrText { get; set; }

    [MaxLength(600)]
    public string? QrObjectKey { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(450)]
    public string? ApprovedByUserId { get; set; }

    [ForeignKey(nameof(ProcurementId))]
    public Procurement Procurement { get; set; } = default!;

    [ForeignKey(nameof(DocumentTypeId))]
    public DocumentType DocumentType { get; set; } = default!;

    public ICollection<ProcDocumentApprovals> Approvals { get; set; } = new List<ProcDocumentApprovals>();
}
