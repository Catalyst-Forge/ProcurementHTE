using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProcDocApprovalFlowRepository : IProcDocApprovalFlowRepository
    {
        private readonly AppDbContext _context;

        public ProcDocApprovalFlowRepository(AppDbContext context) => _context = context;

        public Task<ProcDocuments?> GetDocumentWithProcurementAsync(
            string procDocumentId,
            CancellationToken ct = default
        )
        {
            return _context
                .ProcDocuments.Include(doc => doc.Procurement)
                .Include(doc => doc.DocumentType)
                .FirstOrDefaultAsync(doc => doc.ProcDocumentId == procDocumentId, ct);
        }

        public Task<JobTypeDocuments?> GetJobTypeDocumentWithApprovalsAsync(
            string jobTypeId,
            string documentTypeId,
            CancellationToken ct = default
        )
        {
            return _context
                .JobTypeDocuments.Include(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                .ThenInclude(da => da.Role)
                .FirstOrDefaultAsync(
                    jobTypeDoc =>
                        jobTypeDoc.JobTypeId == jobTypeId
                        && jobTypeDoc.DocumentTypeId == documentTypeId,
                    ct
                );
        }

        public async Task<List<DocumentApprovalRule>> GetConditionalRulesAsync(
            string documentTypeId,
            string? jobTypeId,
            ProcurementHTE.Core.Enums.ProcurementCategory? category,
            CancellationToken ct = default
        )
        {
            try
            {
                var query = _context
                    .DocumentApprovalRules.AsNoTracking()
                    .Where(r => r.DocumentTypeId == documentTypeId && r.IsActive);

                if (!string.IsNullOrWhiteSpace(jobTypeId))
                    query = query.Where(r => r.JobTypeId == null || r.JobTypeId == jobTypeId);

                if (category.HasValue)
                    query = query.Where(
                        r =>
                            r.ProcurementCategory == null
                            || r.ProcurementCategory == category.Value
                    );

                return await query
                    .OrderBy(r => r.MinAmount)
                    .ThenBy(r => r.Sequence)
                    .ToListAsync(ct);
            }
            catch
            {
                // Jika tabel belum tersedia, fallback ke kosong (akan pakai config statis).
                return [];
            }
        }

        public async Task AddApprovalsAsync(
            IEnumerable<ProcDocumentApprovals> approvals,
            CancellationToken ct = default
        )
        {
            await _context.ProcDocumentApprovals.AddRangeAsync(approvals, ct);
        }

        public async Task UpdateProcDocumentStatusAsync(
            string procDocumentId,
            string newStatus,
            CancellationToken ct = default
        )
        {
            var doc = await _context.ProcDocuments.FirstOrDefaultAsync(
                doc => doc.ProcDocumentId == procDocumentId,
                ct
            );
            if (doc != null)
            {
                doc.Status = newStatus;
                _context.ProcDocuments.Update(doc);
            }
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }
    }
}
