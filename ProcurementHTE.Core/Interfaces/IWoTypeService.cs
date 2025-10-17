using ProcurementHTE.Core.Models;


namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeService
    {
        Task<IEnumerable<WoTypes>> GetAllWoTypessAsync();
        Task<WoTypes?> GetWoTypesByIdAsync(string id);
        Task AddWoTypesAsync(WoTypes woType);
        Task EditWoTypesAsync(WoTypes woType, string id);
        Task DeleteWoTypesAsync(WoTypes woType);

    }
}
