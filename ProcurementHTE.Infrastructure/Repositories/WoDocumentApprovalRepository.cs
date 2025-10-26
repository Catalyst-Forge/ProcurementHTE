using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WoDocumentApprovalRepository : IWoDocumentApprovalRepository
    {
        private readonly AppDbContext _context;

        public WoDocumentApprovalRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetByWoDocumentIdAsync(string woDocumentId) {
            return await _context.WoDocumentApprovals
            .Where(docApproval => docApproval.WoDocumentId == woDocumentId)
            .Include(docApproval => docApproval.Role)
            .AsNoTracking()
            .ToListAsync();
        }

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedByWoDocumentIdAsync(string woDocumentId)
        {
            return await _context
                .WoDocumentApprovals.Where(woDoc => woDoc.WoDocumentId == woDocumentId)
                .Include(woDoc => woDoc.Role)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<WoDocumentApprovals> rows) {
            await _context.WoDocumentApprovals.AddRangeAsync(rows);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
