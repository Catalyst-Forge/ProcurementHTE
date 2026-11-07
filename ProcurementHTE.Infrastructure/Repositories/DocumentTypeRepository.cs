using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly AppDbContext _context;
        private readonly IListSortProvider _listSort;

        public DocumentTypeRepository(AppDbContext context, IListSortProvider listSort)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
        }

        public Task<PagedResult<DocumentType>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = _context.DocumentTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
            {
                var s = search.Trim();
                bool byName = fields.Contains("Name");
                bool byDesc = fields.Contains("Description");

                query = query.Where(d =>
                    (byName && d.Name != null && d.Name.Contains(s))
                    || (byDesc && d.Description != null && d.Description.Contains(s))
                );
            }

            return query.ToPagedResultAsync(
                page,
                pageSize,
                q =>
                    q.SortBy(
                        _listSort.Current,
                        ListSortColumns.DocumentTypes,
                        d => d.DocumentTypeId,
                        o => o.OrderBy(d => d.Name)
                    ),
                ct
            );
        }

        public async Task<DocumentType?> GetByIdAsync(string id)
        {
            return await _context.DocumentTypes.FirstOrDefaultAsync(d => d.DocumentTypeId == id);
        }

        public async Task CreateDocumentTypeAsync(DocumentType documentType)
        {
            await _context.DocumentTypes.AddAsync(documentType);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDocumentTypeAsync(DocumentType documentType)
        {
            _context.Entry(documentType).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DropDocumentTypeAsync(DocumentType documentType)
        {
            await MasterDataDeletion.DeleteDocumentTypeAsync(_context, documentType.DocumentTypeId);
            await _context.SaveChangesAsync();
        }
    }
}
