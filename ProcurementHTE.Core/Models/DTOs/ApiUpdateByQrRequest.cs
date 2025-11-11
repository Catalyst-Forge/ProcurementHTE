namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class UpdateByQrRequest
    {
        public string? QrText { get; set; }
        public string? Action { get; set; }   // "approve" | "reject"
        public string? Note { get; set; }
    }
}
