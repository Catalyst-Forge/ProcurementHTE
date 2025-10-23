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

        public async Task<WoDocuments?> GetByIdAsync(string id)
        {
            return await _context.WoDocuments
                .FirstOrDefaultAsync(d => d.WoDocumentId == id);
        }

        public async Task<IReadOnlyList<WoDocuments>> GetByWorkOrderAsync(string workOrderId)
        {
            return await _context.WoDocuments
                .AsNoTracking()
                .Where(d => d.WorkOrderId == workOrderId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(WoDocuments doc)
        {
            await _context.WoDocuments.AddAsync(doc);
        }

        public async Task UpdateAsync(WoDocuments doc)
        {
            _context.WoDocuments.Update(doc);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _context.WoDocuments.FirstOrDefaultAsync(d => d.WoDocumentId == id);
            if (entity != null)
            {
                _context.WoDocuments.Remove(entity);

            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
