namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UpdateByApprovalIdRequest
    {
        public string? ProcDocumentApprovalId { get; set; }
        public string? Action { get; set; }   // "approve" | "reject"
        public string? Note { get; set; }
    }
}
