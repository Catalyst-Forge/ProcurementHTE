using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementService
    {
        // Query Methods
        Task<PagedResult<Procurement>> GetAllProcurementWithDetailsAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct,
            string? userId = null
        );
        Task<Procurement?> GetProcurementByIdAsync(string id);
        Task<IReadOnlyList<Procurement>> GetMyRecentProcurementAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAllProcurementsAsync(CancellationToken ct);

        // Lookup Methods
        Task<(
            List<JobTypes> JobTypes,
            List<Status> Statuses
        )> GetRelatedEntitiesForProcurementAsync();
        Task<JobTypes?> GetJobTypeByIdAsync(string id);
        Task<Status?> GetStatusByNameAsync(string name);

        // Command Methods
        Task AddProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task EditProcurementAsync(
            Procurement procurement,
            string id,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task DeleteProcurementAsync(Procurement procurement);
        Task MarkAsCompletedAsync(string procurementId);
    }
}
