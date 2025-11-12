using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderService
    {
        // Query Methods
        Task<PagedResult<WorkOrder>> GetAllWorkOrderWithDetailsAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<WorkOrder?> GetWorkOrderByIdAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetMyRecentWorkOrderAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAllWoAsync(CancellationToken ct);

        // Lookup Methods
        Task<(List<WoTypes> WoTypes, List<Status> Statuses)> GetRelatedEntitiesForWorkOrderAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(string id);
        Task<Status?> GetStatusByNameAsync(string name);

        // Command Methods
        Task AddWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        );
        Task EditWorkOrderAsync(
            WorkOrder wo,
            string id,
            List<WoDetail> details,
            List<WoOffer> offers
        );
        Task DeleteWorkOrderAsync(WorkOrder wo);
        Task MarkAsCompletedAsync(string woId);
    }
}
