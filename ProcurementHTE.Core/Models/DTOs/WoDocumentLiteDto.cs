namespace ProcurementHTE.Core.Models.DTOs;

public record WoDocumentLiteDto(
    string WoDocumentId,
    string WorkOrderId,
    string FileName,
    string Status,
    string QrText,
    string ObjectKey,
    string? Description,
    DateTime CreatedAt
);
