namespace ProcurementHTE.Core.Authorization.Resources {
    public sealed class ApproveDocContext {
        public string WoDocumentId { get; set; } = null!;
        public decimal TotalPenawaran { get; set; }
    }
}