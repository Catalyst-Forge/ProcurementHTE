using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            var normalized = name.Trim();
            return await _context
                .Statuses.AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.StatusName != null && EF.Functions.Like(status.StatusName, normalized)
                );
        }

        public Task<List<Status>> GetStatusesAsync()
        {
            return _context
                .Statuses.AsNoTracking()
                .OrderBy(status => status.StatusName)
                .ToListAsync();
        }

        public Task<JobTypes?> GetJobTypeByIdAsync(string id) =>
            _context.JobTypes.FirstOrDefaultAsync(job => job.JobTypeId == id);

        public Task<List<JobTypes>> GetJobTypesAsync() =>
            _context.JobTypes.OrderBy(job => job.TypeName).ToListAsync();
    }
}
