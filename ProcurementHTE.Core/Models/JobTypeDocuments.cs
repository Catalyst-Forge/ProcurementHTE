using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

public class JobTypeDocuments
{
    [Key]
    public string JobTypeDocumentId { get; set; } = Guid.NewGuid().ToString();

    public bool IsMandatory { get; set; } = true;

    public int Sequence { get; set; }

    public bool IsGenerated { get; set; }

    public bool IsUploadRequired { get; set; }

    public bool RequiresApproval { get; set; }

    public string? Note { get; set; }

    [Required]
    public string JobTypeId { get; set; } = null!;

    [Required]
    public string DocumentTypeId { get; set; } = null!;

    [ForeignKey(nameof(JobTypeId))]
    public JobTypes JobType { get; set; } = default!;

    [ForeignKey(nameof(DocumentTypeId))]
    public DocumentType DocumentType { get; set; } = default!;

    public ICollection<DocumentApprovals> DocumentApprovals { get; set; } = [];
}
