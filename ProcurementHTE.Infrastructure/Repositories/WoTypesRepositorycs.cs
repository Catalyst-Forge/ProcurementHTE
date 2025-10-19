using Azure;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WoTypesRepository : IWoTypeRepository
    {
        private readonly AppDbContext _context;

        public WoTypesRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateWoTypeAsync(WoTypes woType)
        {
            await _context.AddAsync(woType);
            await _context.SaveChangesAsync();
        }

        public async Task DropWoTypeAsync(WoTypes woType)
        {
            _context.WoTypes.Remove(woType);
            await _context.SaveChangesAsync();
        }

        public Task<PagedResult<WoTypes>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = _context.WoTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
            {
                var s = search.Trim();
                bool byTypeName = fields.Contains("TypeName");
                bool byDesc = fields.Contains("Description");

                query = query.Where(type =>
                    (byTypeName && type.TypeName != null & type.TypeName!.Contains(s))
                    || (byDesc && type.Description != null && type.Description.Contains(s))
                );
            }

            return query.ToPagedResultAsync(page, pageSize, null, ct);
        }

        public async Task<WoTypes?> GetByIdAsync(string id)
        {
            return await _context.WoTypes.FirstOrDefaultAsync(w => w.WoTypeId == id);
        }

        public async Task UpdateWoTypeAsync(WoTypes woType)
        {
            _context.Entry(woType).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
