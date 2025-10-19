using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeService
    {
        Task<PagedResult<WoTypes>> GetAllWoTypessAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<WoTypes?> GetWoTypesByIdAsync(string id);
        Task AddWoTypesAsync(WoTypes woType);
        Task EditWoTypesAsync(WoTypes woType, string id);
        Task DeleteWoTypesAsync(WoTypes woType);
    }
}
