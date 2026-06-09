using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IVendorQueryService
{
    Task<IEnumerable<Vendor>> GetAllVendorsAsync();
    Task<Vendor?> GetVendorByIdAsync(string id);
    Task<PagedResult<Vendor>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    );
}
