using Azure;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class JobTypesRepository : IJobTypeRepository
    {
        private readonly AppDbContext _context;

        public JobTypesRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task CreateJobTypeAsync(JobTypes jobType)
        {
            await _context.AddAsync(jobType);
            await _context.SaveChangesAsync();
        }

        public async Task DropJobTypeAsync(JobTypes jobType)
        {
            _context.JobTypes.Remove(jobType);
            await _context.SaveChangesAsync();
        }

        public Task<PagedResult<JobTypes>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = _context.JobTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
            {
                var s = search.Trim();
                bool byTypeName = fields.Contains("TypeName");
                bool byDesc = fields.Contains("Description");

                query = query.Where(type =>
                    (byTypeName && type.TypeName != null & type.TypeName!.Contains(s))
                    || (byDesc && type.Description != null && type.Description.Contains(s))
                );
            }

            return query.ToPagedResultAsync(page, pageSize, null, ct);
        }

        public async Task<JobTypes?> GetByIdAsync(string id)
        {
            return await _context.JobTypes.FirstOrDefaultAsync(w => w.JobTypeId == id);
        }

        public async Task UpdateJobTypeAsync(JobTypes jobType)
        {
            _context.Entry(jobType).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
