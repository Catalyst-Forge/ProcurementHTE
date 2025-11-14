using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementRepository
    {
        // Query Methods
        Task<Common.PagedResult<Procurement>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<Procurement?> GetByIdAsync(string id);
        Task<Procurement?> GetWithSelectedOfferAsync(string id);
        Task<IReadOnlyList<Procurement>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        );
        Task<int> CountAsync(CancellationToken ct);
        Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByStatusAsync();

        // Lookup Methods
        Task<Status?> GetStatusByNameAsync(string name);
        Task<List<Status>> GetStatusesAsync();
        Task<JobTypes?> GetJobTypeByIdAsync(string id);
        Task<List<JobTypes>> GetJobTypesAsync();

        // Command Methods
        Task StoreProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task UpdateProcurementAsync(Procurement procurement);
        Task UpdateProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        );
        Task DropProcurementAsync(Procurement procurement);
    }
}
