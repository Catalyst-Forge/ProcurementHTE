namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementWorkflowService
    {
        Task MarkAsCompletedAsync(string procurementId);
        Task ApproveByAppoAsync(string procurementId, string appoUserId);
        Task RejectByAppoAsync(string procurementId);
        Task PublishAsync(string procurementId);
        Task UnpublishAsync(string procurementId);
        Task PickupAsync(string procurementId, string appoUserId);
        Task UpdateAccrualDataAsync(
            string procurementId,
            string? noAccrual,
            decimal? potensiAccrual,
            string? statusAccrual,
            string filledByUserId
        );
        Task PickupForApInvoiceAsync(string procurementId, string apInvoiceUserId);
        Task UpdateInvoiceDataAsync(
            string procurementId,
            string? saNo,
            string? sp3No,
            string filledByUserId
        );
        Task PickupForArAsync(string procurementId, string arUserId);
    }
}
