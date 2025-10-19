using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IVendorRepository
    {
        Task<List<Vendor>> GetAllAsync();
        Task<Vendor?> GetByIdAsync(string id);
        Task<string?> GetLastCodeAsync(string prefix);
        Task<IEnumerable<Vendor>> GetAllWithOffersAsync();
        Task<PagedResult<Vendor>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct = default
        );
        Task<int> CountAsync();
        Task StoreVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DropVendorAsync(Vendor vendor);
    }
}
