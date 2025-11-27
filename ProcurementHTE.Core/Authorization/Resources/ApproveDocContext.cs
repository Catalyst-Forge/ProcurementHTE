namespace ProcurementHTE.Core.Authorization.Resources
{
    public sealed class ApproveDocContext
    {
        public string ProcDocumentId { get; set; } = null!;
        public decimal TotalPenawaran { get; set; }
    }
}
