namespace ProcurementHTE.Core.Models.DTOs
{
    public record WoDocumentLiteWithUrlDto(
        string WoDocumentId,
        string WorkOrderId,
        string FileName,
        string Status,
        string QrText,
        string ObjectKey,
        string? Description,
        string? CreatedByUserId,
        string? CreatedByUserName,
        DateTime CreatedAt,
        string? ViewUrl
    )
        : WoDocumentLiteDto(
            WoDocumentId,
            WorkOrderId,
            FileName,
            Status,
            QrText,
            ObjectKey,
            Description,
            CreatedByUserId,
            CreatedByUserName,
            CreatedAt
        );
}
