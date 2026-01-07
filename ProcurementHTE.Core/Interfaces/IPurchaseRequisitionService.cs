using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IPurchaseRequisitionService
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
        int limit = 10,
        CancellationToken ct = default
    );

    Task<int> CountAsync(CancellationToken ct = default);

    // Command Methods
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
