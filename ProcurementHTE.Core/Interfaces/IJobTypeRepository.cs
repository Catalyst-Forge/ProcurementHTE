using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IJobTypeRepository
    {
        Task<PagedResult<JobTypes>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<JobTypes?> GetByIdAsync(string id);
        Task CreateJobTypeAsync(JobTypes jobType);
        Task UpdateJobTypeAsync(JobTypes jobType);
        Task DropJobTypeAsync(JobTypes jobType);
    }
}
