using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories {
    public class WoDocApprovalFlowRepository : IWoDocApprovalFlowRepository {
        private readonly AppDbContext _context;

        public WoDocApprovalFlowRepository(AppDbContext context) => _context = context;

        public Task<WoDocuments?> GetDocumentWithWorkOrderAsync(string woDocumentId, CancellationToken ct = default) {
            return _context.WoDocuments.Include(doc => doc.WorkOrder)
            .FirstOrDefaultAsync(doc => doc.WoDocumentId == woDocumentId, ct);
        }

        public Task<WoTypeDocuments?> GetWoTypeDocumentWithApprovalsAsync(string woTypeId, string documentTypeId, CancellationToken ct = default) {
            return _context.WoTypesDocuments
                .Include(woTypeDoc => woTypeDoc.DocumentApprovals)
                .ThenInclude(da => da.Role)
                .FirstOrDefaultAsync(woTypeDoc => woTypeDoc.WoTypeId == woTypeId && woTypeDoc.DocumentTypeId == documentTypeId, ct);
        }

        public async Task AddApprovalsAsync(IEnumerable<WoDocumentApprovals> approvals, CancellationToken ct = default) {
            await _context.WoDocumentApprovals.AddRangeAsync(approvals, ct);
        }

        public async Task UpdateWoDocumentStatusAsync(string woDocumentId, string newStatus, CancellationToken ct = default) {

            var doc = await _context.WoDocuments.FirstOrDefaultAsync(doc => doc.WoDocumentId == woDocumentId, ct);
            if (doc != null) {
                doc.Status = newStatus;
                _context.WoDocuments.Update(doc);
            }
        }

        public Task SaveChangesAsync(CancellationToken ct = default) {
            return _context.SaveChangesAsync(ct);
        }
    }
}
