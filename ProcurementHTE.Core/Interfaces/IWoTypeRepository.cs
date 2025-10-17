using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeRepository
    {
        Task<IEnumerable<WoTypes>> GetAllAsync();
        Task<WoTypes?> GetByIdAsync(string id);
        Task CreateWoTypeAsync(WoTypes woType);
        Task UpdateWoTypeAsync(WoTypes woType);
        Task DropWoTypeAsync(WoTypes woType);
    }
}
