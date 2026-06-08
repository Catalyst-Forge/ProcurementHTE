using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<AccrualStatistics> GetAccrualStatisticsAsync(
            CancellationToken ct = default
        )
        {
            var totalProcurements = await _context.Procurements.CountAsync(ct);
            var filledCount = await _context.Procurements.CountAsync(
                p => p.NoAccrual != null && p.NoAccrual != "",
                ct
            );
            var pendingCount = totalProcurements - filledCount;
            var totalPotensiAccrual = await _context
                .Procurements.Where(p => p.PotensiAccrual.HasValue)
                .SumAsync(p => p.PotensiAccrual ?? 0m, ct);

            return new AccrualStatistics(pendingCount, filledCount, totalPotensiAccrual);
        }

        public async Task<List<RegionDistribution>> GetRegionDistributionAsync(
            CancellationToken ct = default
        )
        {
            var procurements = await _context
                .Procurements.Where(p => !p.IsDeleted)
                .Select(p => new { p.ProcurementId, Region = p.ProjectRegion })
                .ToListAsync(ct);

            var revenueData = await _context
                .ProfitLosses.Where(pl => !pl.IsDeleted)
                .Select(pl => new
                {
                    pl.ProcurementId,
                    Revenue = pl.Items.Sum(item => (decimal?)item.Revenue) ?? 0m,
                })
                .ToListAsync(ct);

            var revenueByProcurement = revenueData
                .GroupBy(r => r.ProcurementId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Revenue));

            return procurements
                .GroupBy(p => p.Region)
                .Select(g => new RegionDistribution(
                    g.Key.ToString(),
                    g.Count(),
                    g.Sum(p => revenueByProcurement.GetValueOrDefault(p.ProcurementId, 0m))
                ))
                .OrderByDescending(r => r.Count)
                .ToList();
        }
    }
}
