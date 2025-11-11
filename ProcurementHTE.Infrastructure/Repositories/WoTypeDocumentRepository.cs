using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    // Implementasi: samakan return type + tambahkan AsSplitQuery + order defensif
    public class WoTypeDocumentRepository : IWoTypeDocumentRepository
    {
        private readonly AppDbContext _context;
        public WoTypeDocumentRepository(AppDbContext context)
            => _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<WoTypeDocuments?> FindByWoTypeAndDocTypeAsync(string woTypeId, string documentTypeId)
        {
            return await _context.WoTypesDocuments
                .Where(woTypeDoc => woTypeDoc.WoTypeId == woTypeId && woTypeDoc.DocumentTypeId == documentTypeId)
                .Include(woTypeDoc => woTypeDoc.WoType)
                .Include(woTypeDoc => woTypeDoc.DocumentType)
                .Include(woTypeDoc => woTypeDoc.DocumentApprovals)
                    .ThenInclude(da => da.Role)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<WoTypeDocuments?> GetByWoTypeAndDocumentTypeAsync(string woTypeId, string documentTypeId)
        {
            return await _context.WoTypesDocuments
                .Where(x => x.WoTypeId == woTypeId && x.DocumentTypeId == documentTypeId)
                .Include(x => x.WoType)
                .Include(x => x.DocumentType)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<WoTypeDocuments>> ListByWoTypeAsync(string woTypeId, CancellationToken ct = default)
        {
            return await _context.WoTypesDocuments
                .AsNoTracking()
                .Where(x => x.WoTypeId == woTypeId)
                .Include(x => x.DocumentApprovals).ThenInclude(a => a.Role)
                .ToListAsync(ct);
        }
    }
}
