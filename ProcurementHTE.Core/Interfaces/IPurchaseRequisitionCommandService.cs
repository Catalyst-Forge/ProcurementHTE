using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IPurchaseRequisitionCommandService
{
    Task<PurchaseRequisition> CreateAsync(
        PurchaseRequisition purchaseRequisition,
        IEnumerable<string> procurementIds,
        CancellationToken ct = default
    );

    Task UpdateAsync(
        PurchaseRequisition purchaseRequisition,
        IEnumerable<string>? procurementIds = null,
        CancellationToken ct = default
    );

    Task DeleteAsync(string id, string deletedByUserId, CancellationToken ct = default);
}
