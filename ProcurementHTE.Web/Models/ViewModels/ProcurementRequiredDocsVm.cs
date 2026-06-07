using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels;

public class ProcurementRequiredDocsVm
{
    public string ProcurementId { get; set; } = default!;
    public string JobTypeId { get; set; } = default!;
    public List<RequiredDocItemDto> Items { get; set; } = [];
}

public class UploadProcDocumentVm
{
    public string ProcurementId { get; set; } = default!;
    public string DocumentTypeId { get; set; } = default!;
    public string? Description { get; set; }
    public IFormFile File { get; set; } = default!;
}

public class ProcurementDocumentRowViewModel
{
    public string ProcurementId { get; init; } = default!;
    public RequiredDocItemDto Item { get; init; } = default!;
    public int RowIndex { get; init; }
    public bool IsPending { get; init; }

    public bool IsUploaded => Item.Uploaded;
    public string Status => Item.Uploaded ? "Uploaded" : (Item.IsMandatory ? "Required" : "Optional");
}
