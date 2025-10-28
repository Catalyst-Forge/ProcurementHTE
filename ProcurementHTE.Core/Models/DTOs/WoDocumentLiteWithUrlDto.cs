namespace ProcurementHTE.Core.Models.DTOs;

// Turunan dari WoDocumentLiteDto + kolom ViewUrl
public record WoDocumentLiteWithUrlDto(
    string WoDocumentId,
    string WorkOrderId,
    string FileName,
    string Status,
    string QrText,
    string ObjectKey,
    string? Description,
    string? CreatedByUserId,
    DateTime CreatedAt,
    string? ViewUrl
) : WoDocumentLiteDto(
    WoDocumentId,
    WorkOrderId,
    FileName,
    Status,
    QrText,
    ObjectKey,
    Description,
    CreatedByUserId,
    CreatedAt
);
