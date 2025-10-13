using ProcurementHTE.Core.Models;


namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeService
    {
        Task<IEnumerable<WoTypes>> GetAllWoTypessAsync();
        Task<WoTypes?> GetWoTypesByIdAsync(int id);
        Task AddWoTypesAsync(WoTypes woType);
        Task EditWoTypesAsync(WoTypes woType, int id);
        Task DeleteWoTypesAsync(WoTypes woType);

    }
}
