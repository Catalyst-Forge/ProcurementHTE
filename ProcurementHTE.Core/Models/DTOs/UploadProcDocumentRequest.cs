namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UploadProcDocumentRequest
    {
        public string ProcurementId { get; init; } = default!;
        public string DocumentTypeId { get; init; } = default!;
        public Stream Content { get; init; } = default!;
        public long Size { get; init; }
        public string FileName { get; init; } = default!;
        public string ContentType { get; init; } = "application/octet-stream";
        public string? Description { get; init; }
        public string? UploadedByUserId { get; init; } // optional
        public DateTime NowUtc { get; init; } = DateTime.UtcNow; // for testability
    }

    public sealed class UploadProcDocumentResult
    {
        public string ProcDocumentId { get; init; } = default!;
        public string ObjectKey { get; init; } = default!;
        public string FileName { get; init; } = default!;
        public long Size { get; init; }
    }
}
