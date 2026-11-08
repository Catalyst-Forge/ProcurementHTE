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
    LdpArchive,
}

public sealed record DeletedRecordItem(
    string Id,
    string Title,
    string? Detail,
    DateTime DeletedAt,
    string? DeletedByName
);

public sealed record PurgePreview(
    DeletedRecordKind Kind,
    string Id,
    string Title,
    string? Detail,
    int RelatedRows,
    int FileCount,
    IReadOnlyList<string> Blockers
);

public sealed record PurgeRequest(
    DeletedRecordKind Kind,
    string Id,
    string Reason,
    string ActorUserId,
    string? IpAddress
)
{
    public const int MinReasonLength = 10;
}

public sealed record PurgeResult(bool Succeeded, string Message)
{
    public static PurgeResult Ok(string message) => new(true, message);

    public static PurgeResult Fail(string message) => new(false, message);
}

public sealed record RestoreResult(bool Succeeded, string Message)
{
    public static RestoreResult Ok(string message) => new(true, message);

    public static RestoreResult Fail(string message) => new(false, message);
}
