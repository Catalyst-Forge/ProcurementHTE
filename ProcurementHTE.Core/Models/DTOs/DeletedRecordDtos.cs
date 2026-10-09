namespace ProcurementHTE.Core.Models.DTOs;

public enum DeletedRecordKind
{
    Procurement,
    PurchaseRequisition,
    Vendor,
    JobType,
    DocumentType,
    JobTypeDocument,
    DocumentApproval,
    DocumentApprovalRule,
    User,
}

public sealed record DeletedRecordItem(
    string Id,
    string Title,
    string? Detail,
    DateTime DeletedAt,
    string? DeletedByName
);

public sealed record RestoreResult(bool Succeeded, string Message)
{
    public static RestoreResult Ok(string message) => new(true, message);

    public static RestoreResult Fail(string message) => new(false, message);
}
