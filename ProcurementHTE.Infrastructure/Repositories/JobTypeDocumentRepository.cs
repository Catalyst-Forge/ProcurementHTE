using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    // Implementasi: samakan return type + tambahkan AsSplitQuery + order defensif
    public class JobTypeDocumentRepository : IJobTypeDocumentRepository
    {
        private readonly AppDbContext _context;

        public JobTypeDocumentRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<JobTypeDocuments?> FindByJobTypeAndDocTypeAsync(
            string jobTypeId,
            string documentTypeId
        )
        {
            return await _context
                .JobTypeDocuments.Where(jobTypeDoc =>
                    jobTypeDoc.JobTypeId == jobTypeId && jobTypeDoc.DocumentTypeId == documentTypeId
                )
                .Include(jobTypeDoc => jobTypeDoc.JobType)
                .Include(jobTypeDoc => jobTypeDoc.DocumentType)
                .Include(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                .ThenInclude(da => da.Role)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<JobTypeDocuments?> GetByJobTypeAndDocumentTypeAsync(
            string jobTypeId,
            string documentTypeId
        )
        {
            return await _context
                .JobTypeDocuments.Where(x =>
                    x.JobTypeId == jobTypeId && x.DocumentTypeId == documentTypeId
                )
                .Include(x => x.JobType)
                .Include(x => x.DocumentType)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<JobTypeDocuments>> ListByJobTypeAsync(
            string jobTypeId,
            ProcurementHTE.Core.Enums.ProcurementCategory? category = null,
            CancellationToken ct = default
        )
        {
            var query = _context
                .JobTypeDocuments.AsNoTracking()
                .Where(x => x.JobTypeId == jobTypeId);

            if (category.HasValue)
            {
                query = query.Where(x =>
                    x.ProcurementCategory == null || x.ProcurementCategory == category.Value
                );
            }

            return await query
                .Include(x => x.DocumentApprovals)
                .ThenInclude(a => a.Role)
                .ToListAsync(ct);
        }
    }
}
