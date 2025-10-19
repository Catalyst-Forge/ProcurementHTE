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

        public VendorRepository(AppDbContext context) => _context = context;

        public async Task<int> CountAsync() =>
            _context.Vendors == null ? 0 : await _context.Vendors.CountAsync();

        public async Task DropVendorAsync(Vendor vendor)
        {
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Vendor>> GetAllAsync() =>
            _context.Vendors == null ? new List<Vendor>() : await _context.Vendors.ToListAsync();

        public async Task<IEnumerable<Vendor>> GetAllWithOffersAsync()
        {
            return await _context.Vendors.Include(v => v.VendorOffers).ToListAsync();
        }

        public Task<Vendor?> GetByIdAsync(string id) =>
            _context.Vendors == null
                ? Task.FromResult<Vendor?>(null)
                : _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);

        public async Task<string?> GetLastCodeAsync(string prefix) // NEW
        {
            if (_context.Vendors == null)
                return null;
            return await _context
                .Vendors.AsNoTracking()
                .Where(v => v.VendorCode.StartsWith(prefix))
                .OrderByDescending(v => v.VendorCode) // aman karena D6 zero-padded
                .Select(v => v.VendorCode)
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
                bool byContactPerson = fields.Contains("ContactPerson");

                query = query.Where(v =>
                    (byVendorCode && v.VendorCode != null && v.VendorCode.Contains(s))
                    || (byVendorName && v.VendorName != null && v.VendorName.Contains(s))
                    || (byContactPerson && v.ContactPerson != null && v.ContactPerson.Contains(s))
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
            await _context.SaveChangesAsync(); // ⬅️ WAJIB supaya tersimpan
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            if (_context.Vendors == null)
                throw new ArgumentNullException(nameof(vendor));
            try
            {
                _context.Entry(vendor).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                bool exists = _context.Vendors?.Any(e => e.VendorId == vendor.VendorId) ?? false;
                if (!exists)
                    throw new KeyNotFoundException("Vendor not found.");
                throw new Exception("An error occurred while updating the vendor.");
            }
        }
    }
}
