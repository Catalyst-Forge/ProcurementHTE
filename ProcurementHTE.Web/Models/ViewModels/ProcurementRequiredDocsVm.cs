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
