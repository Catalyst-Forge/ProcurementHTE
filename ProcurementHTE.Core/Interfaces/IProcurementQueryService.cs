using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcurementQueryService
    {
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
        Task<PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<PagedResult<Procurement>> GetMyAppoPickupsAsync(
            string appoUserId,
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<(
            List<JobTypes> JobTypes,
            List<Status> Statuses
        )> GetRelatedEntitiesForProcurementAsync();
        Task<JobTypes?> GetJobTypeByIdAsync(string id);
        Task<Status?> GetStatusByNameAsync(string name);
        Task<PagedResult<Procurement>> GetProcurementsForAccrualAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );
        Task<PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );
        Task<PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
            string apInvoiceUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );
        Task<PagedResult<Procurement>> GetProcurementsForArPickupAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        );
        Task<PagedResult<Procurement>> GetMyArPickupsAsync(
            string arUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        );
    }
}
