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

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedByWoDocumentIdAsync(string woDocumentId)
        {
            return await _context
                .WoDocumentApprovals.Where(woDoc => woDoc.WoDocumentId == woDocumentId)
                .Include(woDoc => woDoc.Role)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
