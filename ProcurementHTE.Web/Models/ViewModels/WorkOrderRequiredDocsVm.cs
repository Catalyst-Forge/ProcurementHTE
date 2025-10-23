using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels;

public class WorkOrderRequiredDocsVm
{
    public string WorkOrderId { get; set; } = default!;
    public string WoTypeId { get; set; } = default!;
    public List<RequiredDocItemDto> Items { get; set; } = new();
}

public class UploadWoDocumentVm
{
    public string WorkOrderId { get; set; } = default!;
    public string DocumentTypeId { get; set; } = default!;
    public string? Description { get; set; }
    public IFormFile File { get; set; } = default!;
}
