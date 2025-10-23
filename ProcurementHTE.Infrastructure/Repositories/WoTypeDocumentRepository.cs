using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WoTypeDocumentRepository : IWoTypeDocumentRepository
    {
        private readonly AppDbContext _context;

        public WoTypeDocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WoTypeDocuments?> FindByWoTypeAndDocTypeAsync(
            string woTypeId,
            string documentTypeId
        )
        {
            return await _context.WoTypesDocuments
                .Where(woTypeDoc => woTypeDoc.WoTypeId == woTypeId && woTypeDoc.DocumentTypeId == documentTypeId)
                .Include(woTypeDoc => woTypeDoc.WoType)
                .Include(woTypeDoc => woTypeDoc.DocumentType)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<WoTypeDocuments?> GetByWoTypeAndDocumentTypeAsync(string woTypeId, string documentTypeId)
        {
            return await _context.WoTypesDocuments
                .Include(x => x.WoType)
                .Include(x => x.DocumentType)
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WoTypeId == woTypeId &&
                    x.DocumentTypeId == documentTypeId);
        }
    }
}
