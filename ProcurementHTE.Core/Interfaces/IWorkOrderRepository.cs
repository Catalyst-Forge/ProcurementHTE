using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderRepository
    {
        Task<IEnumerable<WorkOrder>> GetAllAsync();
        Task<WorkOrder?> GetByIdAsync(string id);
        Task StoreWorkOrderAsync(WorkOrder wo);
        Task UpdateWorkOrderAsync(WorkOrder wo);
        Task DropWorkOrderAsync(WorkOrder wo);
        Task<List<WoTypes>> GetWoTypesAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(int id);
        Task<List<Status>> GetStatusesAsync();
    }
}
