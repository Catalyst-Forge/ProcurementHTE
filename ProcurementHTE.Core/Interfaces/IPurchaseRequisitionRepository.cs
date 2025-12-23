using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IPurchaseRequisitionRepository
{
    // Query Methods
    Task<PagedResult<PurchaseRequisition>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct = default
    );

    Task<PurchaseRequisition?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<PurchaseRequisition?> GetByIdWithProcurementsAsync(
        string id,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<PurchaseRequisition>> GetRecentAsync(
        int limit,
        CancellationToken ct = default
    );

    Task<int> CountAsync(CancellationToken ct = default);

    Task<string?> GetLastPrNumberAsync(string prefix, CancellationToken ct = default);

    // Command Methods
    Task CreateAsync(PurchaseRequisition purchaseRequisition, CancellationToken ct = default);

    Task UpdateAsync(PurchaseRequisition purchaseRequisition, CancellationToken ct = default);

    Task DeleteAsync(PurchaseRequisition purchaseRequisition, CancellationToken ct = default);

    Task LinkProcurementsAsync(
        string prId,
        IEnumerable<string> procurementIds,
        CancellationToken ct = default
    );

    Task UnlinkAllProcurementsAsync(string prId, CancellationToken ct = default);
}
