using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IPurchaseRequisitionQueryService
{
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

    Task<bool> IsPrNumberExistsAsync(string prNumber, string? excludePrId = null, CancellationToken ct = default);
}
