namespace ProcurementHTE.Core.Models.DTOs {
    public sealed class GeneratedProcDocumentRequest {
        public string ProcurementId { get; set; } = null!;
        public string DocumentTypeId { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = "application/pdf";
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public string? Description { get; set; }
        public string? GeneratedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
