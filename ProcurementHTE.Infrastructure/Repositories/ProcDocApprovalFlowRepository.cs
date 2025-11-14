using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories {
    public class ProcDocApprovalFlowRepository : IProcDocApprovalFlowRepository {
        private readonly AppDbContext _context;

        public ProcDocApprovalFlowRepository(AppDbContext context) => _context = context;

        public Task<ProcDocuments?> GetDocumentWithProcurementAsync(string procDocumentId, CancellationToken ct = default) {
            return _context.ProcDocuments.Include(doc => doc.Procurement)
            .FirstOrDefaultAsync(doc => doc.ProcDocumentId == procDocumentId, ct);
        }

        public Task<JobTypeDocuments?> GetJobTypeDocumentWithApprovalsAsync(string jobTypeId, string documentTypeId, CancellationToken ct = default) {
            return _context.JobTypeDocuments
                .Include(jobTypeDoc => jobTypeDoc.DocumentApprovals)
                .ThenInclude(da => da.Role)
                .FirstOrDefaultAsync(jobTypeDoc => jobTypeDoc.JobTypeId == jobTypeId && jobTypeDoc.DocumentTypeId == documentTypeId, ct);
        }

        public async Task AddApprovalsAsync(IEnumerable<ProcDocumentApprovals> approvals, CancellationToken ct = default) {
            await _context.ProcDocumentApprovals.AddRangeAsync(approvals, ct);
        }

        public async Task UpdateProcDocumentStatusAsync(string procDocumentId, string newStatus, CancellationToken ct = default) {

            var doc = await _context.ProcDocuments.FirstOrDefaultAsync(doc => doc.ProcDocumentId == procDocumentId, ct);
            if (doc != null) {
                doc.Status = newStatus;
                _context.ProcDocuments.Update(doc);
            }
        }

        public Task SaveChangesAsync(CancellationToken ct = default) {
            return _context.SaveChangesAsync(ct);
        }
    }
}
