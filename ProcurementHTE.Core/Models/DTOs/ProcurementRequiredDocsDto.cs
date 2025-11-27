namespace ProcurementHTE.Core.Models.DTOs;

public class ProcurementRequiredDocsDto
{
    public string ProcurementId { get; set; } = default!;
    public string JobTypeId { get; set; } = default!;
    public List<RequiredDocItemDto> Items { get; set; } = new();
}
