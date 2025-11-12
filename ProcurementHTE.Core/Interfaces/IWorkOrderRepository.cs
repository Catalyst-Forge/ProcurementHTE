using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderRepository
    {
        // Query Methods
        Task<Common.PagedResult<WorkOrder>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<WorkOrder?> GetByIdAsync(string id);
        Task<WorkOrder?> GetWithSelectedOfferAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAsync(CancellationToken ct);
        Task<IReadOnlyList<WoStatusCountDto>> GetCountByStatusAsync();

        // Lookup Methods
        Task<Status?> GetStatusByNameAsync(string name);
        Task<List<Status>> GetStatusesAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(string id);
        Task<List<WoTypes>> GetWoTypesAsync();

        // Command Methods
        Task StoreWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        );
        Task UpdateWorkOrderAsync(WorkOrder wo);
        Task UpdateWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        );
        Task DropWorkOrderAsync(WorkOrder wo);
    }
}
