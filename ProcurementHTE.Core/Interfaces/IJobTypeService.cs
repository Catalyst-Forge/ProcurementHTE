using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IJobTypeService
    {
        Task<PagedResult<JobTypes>> GetAllJobTypessAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<JobTypes?> GetJobTypesByIdAsync(string id);
        Task AddJobTypesAsync(JobTypes jobType);
        Task EditJobTypesAsync(JobTypes jobType, string id);
        Task DeleteJobTypesAsync(JobTypes jobType);
    }
}
