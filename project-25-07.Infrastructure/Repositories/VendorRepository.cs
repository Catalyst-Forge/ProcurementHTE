using Microsoft.EntityFrameworkCore;
using project_25_07.Core.Interfaces;
using project_25_07.Core.Models;
using project_25_07.Infrastructure.Data;

namespace project_25_07.Infrastructure.Repositories {
  public class VendorRepository : IVendorRepository {
    private readonly AppDbContext _context;
    
    public VendorRepository(AppDbContext context) {
      _context = context;
    }

    public async Task<IEnumerable<Vendor>> GetAllAsync() {
      return await _context.Vendors.ToListAsync();
    }

    public async Task<Vendor?> GetByIdAsync(string id) {
      return await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id);
    }

    public async Task CreateVendorAsync(Vendor vendor) {
      await _context.AddAsync(vendor);
      await _context.SaveChangesAsync();
    }

    public async Task UpdateVendorAsync(Vendor vendor, string id) {
      //
    }

    public async Task DropVendorAsync(Vendor vendor, string id) {
      //
    }
  }
}
