namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UploadWoDocumentRequest
    {
        public string WorkOrderId { get; init; } = default!;
        public string DocumentTypeId { get; init; } = default!;
        public Stream Content { get; init; } = default!;
        public long Size { get; init; }
        public string FileName { get; init; } = default!;
        public string ContentType { get; init; } = "application/octet-stream";
        public string? Description { get; init; }
        public string? UploadedByUserId { get; init; } // optional
        public DateTime NowUtc { get; init; } = DateTime.UtcNow; // for testability
    }

    public sealed class UploadWoDocumentResult
    {
        public string WoDocumentId { get; init; } = default!;
        public string ObjectKey { get; init; } = default!;
        public string FileName { get; init; } = default!;
        public long Size { get; init; }
    }
}
