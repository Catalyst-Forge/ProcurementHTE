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
                .ProfitLosses.Include(pnl => pnl.Items)
                .ThenInclude(item => item.ProcOffer)
                .FirstOrDefaultAsync(pnl => pnl.ProfitLossId == profitLossId);
        }

        public async Task<ProfitLoss?> GetByProcurementAsync(string procurementId)
        {
            return await _context
                .ProfitLosses.Include(pnl => pnl.Items)
                .ThenInclude(item => item.ProcOffer)
                .AsNoTracking()
                .FirstOrDefaultAsync(pnl => pnl.ProcurementId == procurementId);
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
                .Include(pnl => pnl.Items)
                .ThenInclude(item => item.ProcOffer)
                .Include(pl => pl.VendorOffers)
                .ThenInclude(vo => vo.Vendor)
                .Include(pl => pl.VendorOffers)
                .ThenInclude(vo => vo.ProcOffer)
                .Where(pnl => pnl.ProcurementId == procurementId)
                .OrderByDescending(pnl => pnl.CreatedAt)
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

        public async Task StoreProfitLossAggregateAsync(
            ProfitLoss profitLoss,
            IEnumerable<string> selectedVendorIds,
            IEnumerable<VendorOffer> vendorOffers
        )
        {
            ArgumentNullException.ThrowIfNull(profitLoss);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RemoveExistingSelectedVendorsAsync(profitLoss.ProcurementId);
                await AddSelectedVendorsAsync(profitLoss.ProcurementId, selectedVendorIds);

                await _context.ProfitLosses.AddAsync(profitLoss);

                var offers = vendorOffers?.ToList() ?? [];
                if (offers.Count > 0)
                    await _context.VendorOffers.AddRangeAsync(offers);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateProfitLossAggregateAsync(
            ProfitLoss profitLoss,
            IEnumerable<string> selectedVendorIds,
            IEnumerable<VendorOffer> vendorOffers
        )
        {
            ArgumentNullException.ThrowIfNull(profitLoss);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RemoveExistingSelectedVendorsAsync(profitLoss.ProcurementId);
                await AddSelectedVendorsAsync(profitLoss.ProcurementId, selectedVendorIds);

                var oldOffers = await _context
                    .VendorOffers.Where(o => o.ProcurementId == profitLoss.ProcurementId)
                    .ToListAsync();
                if (oldOffers.Count > 0)
                    _context.VendorOffers.RemoveRange(oldOffers);

                var offers = vendorOffers?.ToList() ?? [];
                if (offers.Count > 0)
                    await _context.VendorOffers.AddRangeAsync(offers);

                _context.ProfitLosses.Update(profitLoss);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task RemoveExistingSelectedVendorsAsync(string procurementId)
        {
            var existing = await _context
                .ProfitLossSelectedVendors.Where(item => item.ProcurementId == procurementId)
                .ToListAsync();

            if (existing.Count > 0)
                _context.ProfitLossSelectedVendors.RemoveRange(existing);
        }

        private Task AddSelectedVendorsAsync(string procurementId, IEnumerable<string> selectedIds)
        {
            var rows = selectedIds
                .Distinct()
                .Select(vendor => new ProfitLossSelectedVendor
                {
                    ProcurementId = procurementId,
                    VendorId = vendor,
                })
                .ToList();

            if (rows.Count == 0)
                return Task.CompletedTask;

            return _context.ProfitLossSelectedVendors.AddRangeAsync(rows);
        }
    }
}
