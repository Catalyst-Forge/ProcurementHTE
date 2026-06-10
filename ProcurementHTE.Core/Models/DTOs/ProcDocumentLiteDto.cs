namespace ProcurementHTE.Core.Models.DTOs;

public record ProcDocumentLiteDto(
    string ProcDocumentId,
    string ProcurementId,
    string FileName,
    string ObjectKey,
    string? Description,
    string? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAt
);
