using Microsoft.EntityFrameworkCore;
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

        public Task<Vendor?> GetByIdAsync(string id) =>
            _context.Vendors == null
                ? Task.FromResult<Vendor?>(null)
                : _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);

        public async Task<string?> GetLastCodeAsync(string prefix) // NEW
        {
            if (_context.Vendors == null) return null;
            return await _context.Vendors
                .AsNoTracking()
                .Where(v => v.VendorCode.StartsWith(prefix))
                .OrderByDescending(v => v.VendorCode)   // aman karena D6 zero-padded
                .Select(v => v.VendorCode)
                .FirstOrDefaultAsync();
        }

        public async Task StoreVendorAsync(Vendor vendor)
        {
            await _context.Vendors.AddAsync(vendor);
            await _context.SaveChangesAsync(); // ⬅️ WAJIB supaya tersimpan
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            if (_context.Vendors == null) throw new ArgumentNullException(nameof(vendor));
            try
            {
                _context.Entry(vendor).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                bool exists = _context.Vendors?.Any(e => e.VendorId == vendor.VendorId) ?? false;
                if (!exists) throw new KeyNotFoundException("Vendor not found.");
                throw new Exception("An error occurred while updating the vendor.");
            }
        }
    }

}
