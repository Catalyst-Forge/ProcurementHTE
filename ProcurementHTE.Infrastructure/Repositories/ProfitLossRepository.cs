using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProfitLossRepository(AppDbContext context) : IProfitLossRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<IEnumerable<ProfitLoss>> GetAllAsync()
        {
            return await _context
                .ProfitLosses.Include(x => x.WorkOrder)
                .Include(x => x.SelectedVendorOffer)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProfitLoss?> GetByIdAsync(string id)
        {
            return await _context
                .ProfitLosses.Include(pnl => pnl.WorkOrder)
                .Include(pnl => pnl.SelectedVendorOffer)
                .ThenInclude(offer => offer.Vendor)
                .FirstOrDefaultAsync(pnl => pnl.ProfitLossId == id);
        }

        public async Task<ProfitLoss?> GetByWorkOrderAsync(string woId)
        {
            return await _context
                .ProfitLosses.Include(profitLoss => profitLoss.WorkOrder)
                .Include(profitLoss => profitLoss.SelectedVendorOffer)
                .ThenInclude(profitLoss => profitLoss.Vendor)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkOrderId == woId);
        }

        public async Task<IEnumerable<ProfitLoss>> GetProfitLossByDateRangeAsync(
            DateTime startDate,
            DateTime endDate
        )
        {
            return await _context
                .ProfitLosses.Include(x => x.WorkOrder)
                .Include(x => x.SelectedVendorOffer)
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
                .ToListAsync();
        }

        public async Task<ProfitLoss> StoreProfitLossAsync(ProfitLoss pnl)
        {
            await _context.AddAsync(pnl);
            await _context.SaveChangesAsync();
            return pnl;
        }

        public async Task UpdateProfitLossAsync(ProfitLoss pnl)
        {
            try
            {
                _context.Entry(pnl).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var pnlExists = await _context.ProfitLosses.AnyAsync(profitLoss =>
                    profitLoss.ProfitLossId == pnl.ProfitLossId
                );
                if (!pnlExists)
                    throw new KeyNotFoundException(
                        $"Profit and Loss dengan ID {pnl.ProfitLossId} tidak ditemukan"
                    );

                throw new InvalidOperationException("Data tidak valid", ex);
            }
        }

        public async Task DropProfitLossAsync(string id)
        {
            var pnl = await _context.ProfitLosses.FindAsync(id);
            if (pnl != null)
            {
                _context.ProfitLosses.Remove(pnl);
                await _context.SaveChangesAsync();
            }
        }
    }
}
