using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderRepository
    {
        // Get Data
        Task<IEnumerable<WorkOrder>> GetAllAsync();
        Task<WorkOrder?> GetByIdAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<Status?> GetStatusByNameAsync(string name);
        Task<List<WoTypes>> GetWoTypesAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(string id);
        Task<List<Status>> GetStatusesAsync();
        Task<WorkOrder?> GetWithOffersAsync(string id);
        Task<WorkOrder?> GetWithSelectedOfferAsync(string id);
        Task<int> CountAsync(CancellationToken ct);

        // Transactions DB
        Task StoreWorkOrderAsync(WorkOrder wo);
        Task StoreWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details);
        Task UpdateWorkOrderAsync(WorkOrder wo);
        Task DropWorkOrderAsync(WorkOrder wo);
    }
}
