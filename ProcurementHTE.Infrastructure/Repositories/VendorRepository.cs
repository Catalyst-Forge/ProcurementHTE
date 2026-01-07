using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly AppDbContext _context;

        public VendorRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<int> CountAsync()
        {
            return _context.Vendors == null ? 0 : await _context.Vendors.CountAsync();
        }

        public async Task<List<Vendor>> GetAllAsync()
        {
            return await _context.Vendors.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Vendor>> GetAllWithOffersAsync()
        {
            return await _context.Vendors.Include(v => v.VendorOffers).AsNoTracking().ToListAsync();
        }

        public Task<Vendor?> GetByIdAsync(string id)
        {
            return _context.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.VendorId == id);
        }

        public async Task<string?> GetLastCodeAsync(string prefix)
        {
            return await _context
                .Vendors.Where(v => v.VendorCode.StartsWith(prefix))
                .OrderByDescending(v => v.VendorCode)
                .Select(v => v.VendorCode)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public Task<PagedResult<Vendor>> GetPagedAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct = default
        )
        {
            var query = _context.Vendors.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
            {
                var s = search.Trim();
                bool byVendorCode = fields.Contains("VendorCode");
                bool byVendorName = fields.Contains("VendorName");

                query = query.Where(v =>
                    (byVendorCode && v.VendorCode != null && v.VendorCode.Contains(s))
                    || (byVendorName && v.VendorName != null && v.VendorName.Contains(s))
                );
            }

            return query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(v => v.CreatedAt),
                ct: ct
            );
        }

        public async Task StoreVendorAsync(Vendor vendor)
        {
            await _context.Vendors.AddAsync(vendor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            _context.Entry(vendor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Vendor vendor, string deletedByUserId)
        {
            var entityToDelete = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == vendor.VendorId);
            if (entityToDelete != null)
            {
                entityToDelete.IsDeleted = true;
                entityToDelete.DeletedAt = DateTime.UtcNow;
                entityToDelete.DeletedBy = deletedByUserId;
                
                // Prepend dash to VendorCode to allow reuse of the same code
                entityToDelete.VendorCode = $"-{entityToDelete.VendorCode}";
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
