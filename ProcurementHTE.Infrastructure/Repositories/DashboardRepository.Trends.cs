using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<List<MonthlyTrend>> GetMonthlyProcurementTrendAsync(
            CancellationToken ct = default
        )
        {
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);

            return await _context
                .Procurements.Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .Where(p => p.CreatedAt >= twelveMonthsAgo)
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new MonthlyTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalValue = g.Sum(p =>
                        p.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m
                    ),
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync(ct);
        }

        public async Task<List<MonthlyTrend>> GetMonthlyPurchaseRequisitionTrendAsync(
            CancellationToken ct = default
        )
        {
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);

            return await _context
                .PurchaseRequisitions.Where(pr => pr.CreatedAt >= twelveMonthsAgo)
                .GroupBy(pr => new { pr.CreatedAt.Year, pr.CreatedAt.Month })
                .Select(g => new MonthlyTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalValue = 0m,
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync(ct);
        }

        public async Task<List<StatusCount>> GetProcurementsByStatusAsync(
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Where(proc => proc.Status != null)
                .GroupBy(proc => proc.Status!.StatusName)
                .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        }

        public async Task<List<StatusCount>> GetDocumentApprovalStatsAsync(
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.Where(pr =>
                    pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager
                    || pr.Status == PurchaseRequisitionStatus.DonePO
                )
                .GroupBy(pr => pr.Status.ToString())
                .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        }
    }
}
