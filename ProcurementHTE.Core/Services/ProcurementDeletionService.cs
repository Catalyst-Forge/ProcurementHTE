using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// One job's delete, shared by the job's own Hapus button and the LDP's Hapus terpilih.
/// The documents and P&amp;L go first: restoring the job brings back what the same person
/// deleted just before it (see DeletedRecordsService).
/// </summary>
public sealed class ProcurementDeletionService(
    IProcurementQueryService queries,
    IProcurementCommandService commands,
    IProcDocumentService documents,
    IProfitLossCommandService profitLoss,
    IPurchaseRequisitionCommandService requisitions,
    IProcurementTrackingService tracking
) : IProcurementDeletionService
{
    public async Task<int?> DeleteAsync(string procurementId, string deletedByUserId)
    {
        var procurement = await queries.GetProcurementByIdAsync(procurementId);
        if (procurement is null)
            return null;

        var prId = procurement.PrId;
        var deletedDocs = await documents.DeleteAllByProcurementAsync(procurementId, deletedByUserId);

        await profitLoss.DeleteByProcurementAsync(procurementId, deletedByUserId);
        await commands.DeleteProcurementAsync(procurement, deletedByUserId);

        if (!string.IsNullOrEmpty(prId) && !await requisitions.DeleteIfEmptyAsync(prId, deletedByUserId))
            await tracking.RecalculatePrStatusAsync(prId);

        return deletedDocs;
    }
}
