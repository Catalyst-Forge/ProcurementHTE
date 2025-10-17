using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<WoTypes>> GetAllAsync()
        {
            return await _context.WoTypes.ToListAsync();
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
