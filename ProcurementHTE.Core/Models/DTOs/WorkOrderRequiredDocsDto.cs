namespace ProcurementHTE.Core.Models.DTOs;

public class WorkOrderRequiredDocsDto
{
    public string WorkOrderId { get; set; } = default!;
    public string WoTypeId { get; set; } = default!;
    public List<RequiredDocItemDto> Items { get; set; } = new();
}
