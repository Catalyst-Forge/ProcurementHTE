using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProfitLossRepository : IProfitLossRepository
    {
        private readonly AppDbContext _context;

        public ProfitLossRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public Task<ProfitLoss?> GetByIdAsync(string profitLossId)
        {
            return _context
                .ProfitLosses.Include(pnl => pnl.WorkOrder)
                .FirstOrDefaultAsync(pnl => pnl.ProfitLossId == profitLossId);
        }

        public async Task<ProfitLoss?> GetByWorkOrderAsync(string woId)
        {
            return await _context
                .ProfitLosses.AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkOrderId == woId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string workOrderId)
        {
            return _context
                .ProfitLossSelectedVendors.Where(x => x.WorkOrderId == workOrderId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProfitLoss?> GetLatestByWorkOrderIdAsync(string workOrderId) {
            return await _context.ProfitLosses
                .AsNoTracking()
                .Where(p => p.WorkOrderId == workOrderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task StoreProfitLossAsync(ProfitLoss profitLoss) {
            await _context.ProfitLosses.AddAsync(profitLoss);
            await _context.SaveChangesAsync();
        }


        public async Task StoreSelectedVendorsAsync(string woId, IEnumerable<string> vendorId)
        {
            var rows = vendorId
                .Distinct()
                .Select(vendor => new ProfitLossSelectedVendor
                {
                    WorkOrderId = woId,
                    VendorId = vendor,
                });

            await _context.ProfitLossSelectedVendors.AddRangeAsync(rows);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveSelectedVendorsAsync(string woId)
        {
            var olds = await _context
                .ProfitLossSelectedVendors.Where(item => item.WorkOrderId == woId)
                .ToListAsync();
            if (olds.Count > 0)
                _context.RemoveRange(olds);
        }

        public Task UpdateProfitLossAsync(ProfitLoss profitLoss)
        {
            _context.ProfitLosses.Update(profitLoss);
            return Task.CompletedTask;
        }
    }
}
