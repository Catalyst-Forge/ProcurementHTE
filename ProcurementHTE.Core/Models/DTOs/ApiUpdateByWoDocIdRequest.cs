namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UpdateByWoDocumentIdRequest
    {
        public string? WoDocumentId { get; set; }
        public string? Action { get; set; }   // "approve" | "reject"
        public string? Note { get; set; }
    }
}
