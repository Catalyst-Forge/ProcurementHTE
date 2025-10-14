using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderRepository
    {
        Task<IEnumerable<WorkOrder>> GetAllAsync();
        Task<WorkOrder?> GetByIdAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetRecentByUserAsync(
            string userId,
            int limit = 10,
            CancellationToken ct = default
        );
        Task<Status?> GetStatusByNameAsync(string name);
        Task<int> CountAsync(CancellationToken ct);
        Task StoreWorkOrderAsync(WorkOrder wo);
        Task StoreWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details);
        Task UpdateWorkOrderAsync(WorkOrder wo);
        Task DropWorkOrderAsync(WorkOrder wo);
        Task<List<WoTypes>> GetWoTypesAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(int id);
        Task<List<Status>> GetStatusesAsync();
    }
}
