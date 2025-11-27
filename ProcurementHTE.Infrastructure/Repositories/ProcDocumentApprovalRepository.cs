using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProcDocumentApprovalRepository : IProcDocumentApprovalRepository
    {
        private readonly AppDbContext _context;

        public ProcDocumentApprovalRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetByProcDocumentIdAsync(
            string procDocumentId
        )
        {
            return await _context
                .ProcDocumentApprovals.Where(docApproval =>
                    docApproval.ProcDocumentId == procDocumentId
                )
                .Include(docApproval => docApproval.Role)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedByProcDocumentIdAsync(
            string procDocumentId
        )
        {
            return await _context
                .ProcDocumentApprovals.Where(procDoc => procDoc.ProcDocumentId == procDocumentId)
                .Include(procDoc => procDoc.Role)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ProcDocumentApprovals> rows)
        {
            await _context.ProcDocumentApprovals.AddRangeAsync(rows);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
