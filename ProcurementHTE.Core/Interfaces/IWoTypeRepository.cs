using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeRepository
    {
        Task<PagedResult<WoTypes>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<WoTypes?> GetByIdAsync(string id);
        Task CreateWoTypeAsync(WoTypes woType);
        Task UpdateWoTypeAsync(WoTypes woType);
        Task DropWoTypeAsync(WoTypes woType);
    }
}
