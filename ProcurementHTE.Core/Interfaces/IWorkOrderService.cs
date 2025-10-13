using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWorkOrderService
    {
        Task<IEnumerable<WorkOrder>> GetAllWorkOrderWithDetailsAsync();
        Task<WorkOrder?> GetWorkOrderByIdAsync(string id);
        Task<IReadOnlyList<WorkOrder>> GetMyRecentWorkOrderAsync(
            string userId,
            int limit = 10,
            CancellationToken ct = default
        );
        Task<int> CountAllWoAsync(CancellationToken ct);
        Task<WoTypes?> GetWoTypeByIdAsync(int id);
        Task<Status?> GetStatusByNameAsync(string name);
        Task AddWorkOrderAsync(WorkOrder wo);
        Task AddWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details);
        Task EditWorkOrderAsync(WorkOrder wo, string id);
        Task DeleteWorkOrderAsync(WorkOrder wo);
        Task<(List<WoTypes> WoTypes, List<Status> Statuses)> GetRelatedEntitiesForWorkOrderAsync();
    }
}
