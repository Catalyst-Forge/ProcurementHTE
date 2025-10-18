using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeRepository
    {
        Task<PagedResult<WoTypes>> GetAllAsync(int page, int pageSize, CancellationToken ct);
        Task<WoTypes?> GetByIdAsync(string id);
        Task CreateWoTypeAsync(WoTypes woType);
        Task UpdateWoTypeAsync(WoTypes woType);
        Task DropWoTypeAsync(WoTypes woType);
    }
}
