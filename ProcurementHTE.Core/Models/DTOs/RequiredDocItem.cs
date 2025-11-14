namespace ProcurementHTE.Core.Models.DTOs;

public class RequiredDocItemDto
{
    public string JobTypeDocumentId { get; set; } = default!;
    public int Sequence { get; set; }
    public string DocumentTypeId { get; set; } = default!;
    public string DocumentTypeName { get; set; } = default!;
    public bool IsMandatory { get; set; }
    public bool IsUploadRequired { get; set; }
    public bool IsGenerated { get; set; }
    public bool RequiresApproval { get; set; }
    public string? Note { get; set; }

    public bool Uploaded { get; set; }
    public string? ProcDocumentId { get; set; }
    public string? FileName { get; set; }
    public long? Size { get; set; }
    public string? Status { get; set; }
}
