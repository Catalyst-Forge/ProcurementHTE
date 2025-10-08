using Microsoft.EntityFrameworkCore;
using project_25_07.Core.Interfaces;
using project_25_07.Core.Models;
using project_25_07.Infrastructure.Data;

namespace project_25_07.Infrastructure.Repositories {
  public class TenderRepository : ITenderRepository {
    private readonly AppDbContext _context;

    public TenderRepository(AppDbContext context) {
      _context = context;
    }

    public async Task<IEnumerable<Tender>> GetAllAsync() {
      return await _context.Tenders.ToListAsync();
    }

    public async Task<Tender?> GetByIdAsync(string id) {
      return await _context.Tenders.FirstOrDefaultAsync(t => t.TenderId == id);
    }

    public async Task CreateTenderAsync(Tender tender) {
      await _context.AddAsync(tender);
      await _context.SaveChangesAsync();
    }

    public async Task UpdateTenderAsync(Tender tender) {
      _context.Entry(tender).State = EntityState.Modified;
      await _context.SaveChangesAsync();
    }

    public async Task DropTenderAsync(Tender tender) {
      _context.Tenders.Remove(tender);
      await _context.SaveChangesAsync();
    }
  }
}
