namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UpdateByProcDocumentIdRequest
    {
        public string? ProcDocumentId { get; set; }
        public string? Action { get; set; }   // "approve" | "reject"
        public string? Note { get; set; }
    }
}
