using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
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
                .ProfitLosses.Include(p => p.Items)
                .Include(p => p.Procurement)
                .ThenInclude(wo => wo.ProcOffers)
                .FirstOrDefaultAsync(p => p.ProfitLossId == profitLossId);
        }

        public async Task<ProfitLoss?> GetByProcurementAsync(string woId)
        {
            return await _context
                .ProfitLosses.Include(p => p.Items)
                .Include(p => p.Procurement)
                .ThenInclude(wo => wo.ProcOffers)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProcurementId == woId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string procurementId)
        {
            return _context
                .ProfitLossSelectedVendors.Where(x => x.ProcurementId == procurementId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProfitLoss?> GetLatestByProcurementIdAsync(string procurementId)
        {
            return await _context
                .ProfitLosses.AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.ProcurementId == procurementId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal> GetTotalRevenueThisMonthAsync()
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, 1);
            var end = start.AddMonths(1);

            return await _context
                .ProfitLossItems.Where(item =>
                    item.ProfitLoss.CreatedAt >= start && item.ProfitLoss.CreatedAt < end
                )
                .SumAsync(item => item.Revenue);
        }

        public async Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year)
        {
            return await _context
                .ProfitLossItems.Where(item => item.ProfitLoss.CreatedAt.Year == year)
                .GroupBy(item => item.ProfitLoss.CreatedAt.Month)
                .Select(group => new RevenuePerMonthDto
                {
                    Month = group.Key,
                    Total = group.Sum(item => item.Revenue),
                })
                .OrderBy(revenue => revenue.Month)
                .ToListAsync();
        }

        public async Task StoreProfitLossAsync(ProfitLoss profitLoss)
        {
            await _context.ProfitLosses.AddAsync(profitLoss);
            await _context.SaveChangesAsync();
        }

        public async Task StoreSelectedVendorsAsync(string woId, IEnumerable<string> vendorId)
        {
            var rows = vendorId
                .Distinct()
                .Select(vendor => new ProfitLossSelectedVendor
                {
                    ProcurementId = woId,
                    VendorId = vendor,
                });

            await _context.ProfitLossSelectedVendors.AddRangeAsync(rows);
            await _context.SaveChangesAsync();
        }

        public Task UpdateProfitLossAsync(ProfitLoss profitLoss)
        {
            _context.ProfitLosses.Update(profitLoss);
            return _context.SaveChangesAsync();
        }

        public async Task RemoveSelectedVendorsAsync(string woId)
        {
            var olds = await _context
                .ProfitLossSelectedVendors.Where(item => item.ProcurementId == woId)
                .ToListAsync();
            if (olds.Count > 0)
            {
                _context.RemoveRange(olds);
                await _context.SaveChangesAsync(); // ⬅️ tambahkan save agar bersih benar
            }
        }
    }
}
