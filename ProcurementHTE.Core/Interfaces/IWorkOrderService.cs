using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderService
    {
        // Get Data
        Task<PagedResult<WorkOrder>> GetAllWorkOrderWithDetailsAsync(int page, int pageSize, CancellationToken ct);
        Task<WorkOrder?> GetWorkOrderByIdAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetMyRecentWorkOrderAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<(List<WoTypes> WoTypes, List<Status> Statuses)> GetRelatedEntitiesForWorkOrderAsync();
        Task<WoTypes?> GetWoTypeByIdAsync(string id);
        Task<Status?> GetStatusByNameAsync(string name);
        Task<WorkOrder?> GetWithOffersAsync(string id);
        Task<int> CountAllWoAsync(CancellationToken ct);

        // Transaction DB
        Task AddWorkOrderAsync(WorkOrder wo);
        Task AddWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details);
        Task EditWorkOrderAsync(WorkOrder wo, string id);
        Task DeleteWorkOrderAsync(WorkOrder wo);
    }
}
