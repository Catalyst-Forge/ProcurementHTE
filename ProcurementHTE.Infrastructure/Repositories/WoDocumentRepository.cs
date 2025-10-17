using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WoDocumentRepository : IWoDocumentRepository
    {
        private readonly AppDbContext _context;

        public WoDocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WoDocuments?> GetByIdWithWorkOrderAsync(string woDocumentId)
        {
            return await _context
                .WoDocuments
                .Where(woDoc => woDoc.WoDocumentId == woDocumentId)
                .Include(woDoc => woDoc.WorkOrder)
                .ThenInclude(wo => wo.Vendor)
                .Include(woDoc => woDoc.DocumentType)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
