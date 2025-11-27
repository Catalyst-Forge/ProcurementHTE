namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ByQrRequest
    {
        public string? QrText { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
