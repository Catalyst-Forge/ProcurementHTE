using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly AppDbContext _context;

        public DocumentTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateDocumentTypeAsync(DocumentType documentType)
        {
            await _context.AddAsync(documentType);
            await _context.SaveChangesAsync();
        }

        public async Task DropDocumentTypeAsync(DocumentType documentType)
        {
            _context.DocumentTypes.Remove(documentType);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DocumentType>> GetAllAsync()
        {
            return await _context.DocumentTypes.ToListAsync();
        }

        public async Task<DocumentType?> GetByIdAsync(string id)
        {
            return await _context.DocumentTypes.FirstOrDefaultAsync(d => d.DocumentTypeId == id);
        }

        public async Task UpdateDocumentTypeAsync(DocumentType documentType)
        {
            _context.Entry(documentType).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
