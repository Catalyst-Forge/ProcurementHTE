using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10)
        {
            var procurements = await _context
                .Procurements.AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .Select(p => new RecentActivityDto
                {
                    Time = p.CreatedAt,
                    User = p.User != null ? p.User.FullName : "Unknown",
                    Action = $"Created Procurement {p.ProcNum}",
                    Description = p.JobName ?? p.Note,
                })
                .ToListAsync();

            var docs = await _context
                .ProcDocuments.AsNoTracking()
                .OrderByDescending(doc => doc.CreatedAt)
                .Take(50)
                .Select(doc => new RecentActivityDto
                {
                    Time = doc.CreatedAt,
                    User = doc.Procurement.User != null ? doc.Procurement.User.FullName : "Unknown",
                    Action = $"Uploaded Document {doc.FileName}",
                    Description = $"For Procurement {doc.Procurement!.ProcNum} Upload Document",
                })
                .ToListAsync();

            var pnl = await _context
                .ProfitLosses.AsNoTracking()
                .OrderByDescending(pnl => pnl.CreatedAt)
                .Take(50)
                .Select(pnl => new RecentActivityDto
                {
                    Time = pnl.CreatedAt,
                    User = pnl.Procurement.User != null ? pnl.Procurement.User.FullName : "Unknown",
                    Action = "Created Profit & Loss Record",
                    Description =
                        $"For Procurement {pnl.Procurement!.ProcNum} Create Profit & Loss Record",
                })
                .ToListAsync();

            return procurements
                .Concat(docs)
                .Concat(pnl)
                .OrderByDescending(activity => activity.Time)
                .Take(take)
                .ToList();
        }

        // GetApprovalStatusCountsAsync removed - approval per-document sudah dihapus

        // Dashboard Metrics
        public async Task<int> GetActiveProcurementsCountAsync(CancellationToken ct = default)
        {
            return await _context
                .Procurements.Where(p => p.Status != null)
                .CountAsync(
                    p => p.Status!.StatusName == "Created" || p.Status!.StatusName == "In Progress",
                    ct
                );
        }

        public async Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default)
        {
            // ProcDocumentApprovals removed - sekarang count pending approvals dari PR status
            return await _context.PurchaseRequisitions.CountAsync(
                pr => pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager,
                ct
            );
        }

        public async Task<int> GetTotalVendorsCountAsync(CancellationToken ct = default)
        {
            return await _context.Vendors.CountAsync(ct);
        }

        public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLossItems.SumAsync(pnl => (decimal?)pnl.Revenue, ct) ?? 0m;
        }

        public async Task<decimal> GetTotalCostAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLosses.SumAsync(
                    pnl => (decimal?)pnl.SelectedVendorFinalOffer,
                    ct
                ) ?? 0m;
        }

        public async Task<decimal> GetTotalProfitAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLosses.SumAsync(pnl => (decimal?)pnl.Profit, ct) ?? 0m;
        }

        // Lists and Details
        public async Task<List<ProcurementSummary>> GetRecentProcurementsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Include(proc => proc.Status)
                .Include(proc => proc.JobType)
                .Include(proc => proc.User)
                .Include(proc => proc.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .OrderByDescending(proc => proc.CreatedAt)
                .Take(take)
                .Select(proc => new ProcurementSummary
                {
                    ProcNum = proc.ProcNum,
                    JobTypeName = proc.JobType != null ? proc.JobType.TypeName : string.Empty,
                    StatusName = proc.Status != null ? proc.Status.StatusName : string.Empty,
                    CreatedBy = proc.User != null ? proc.User.FullName! : string.Empty,
                    CreatedDate = proc.CreatedAt,
                    TotalAmount =
                        proc.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m,
                })
                .ToListAsync(ct);
        }

        public async Task<List<ApprovalSummary>> GetPendingApprovalsDetailAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            // ProcDocumentApprovals removed - sekarang pending approvals dari PR status
            return await _context
                .PurchaseRequisitions
                .Include(pr => pr.Procurements)
                .Where(pr => pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager)
                .OrderByDescending(pr => pr.CreatedAt)
                .Take(take)
                .Select(pr => new ApprovalSummary
                {
                    ProcNum = pr.Procurements.FirstOrDefault() != null 
                        ? pr.Procurements.First().ProcNum 
                        : "-",
                    DocumentName = pr.PrNumber ?? "-",
                    ApprovalRole = pr.Status.ToString(),
                    CreatedDate = pr.CreatedAt,
                })
                .ToListAsync(ct);
        }

        public async Task<List<JobTypeCount>> GetJobTypeDistributionAsync(
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Include(proc => proc.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .Where(proc => proc.JobType != null)
                .GroupBy(proc => proc.JobType!.TypeName)
                .Select(g => new JobTypeCount
                {
                    JobTypeName = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(proc =>
                        proc.ProfitLosses.SelectMany(pl => pl.Items)
                            .Sum(item => (decimal?)item.Revenue)
                        ?? 0m
                    ),
                })
                .OrderByDescending(j => j.Count)
                .ToListAsync(ct);
        }

        public async Task<List<VendorPerformance>> GetTopVendorsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .Vendors.Select(v => new VendorPerformance
                {
                    VendorCode = v.VendorCode,
                    VendorName = v.VendorName,
                    OfferCount = _context.VendorOffers.Count(vo => vo.VendorId == v.VendorId),
                    SelectedCount = _context.ProfitLosses.Count(pl =>
                        pl.SelectedVendorId == v.VendorId
                    ),
                })
                .Where(v => v.OfferCount > 0)
                .OrderByDescending(v => v.OfferCount)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task<List<PurchaseRequisitionSummary>> GetRecentPurchaseRequisitionsAsync(
            int take = 10,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
                .Include(pr => pr.Procurements)
                .OrderByDescending(pr => pr.CreatedAt)
                .Take(take)
                .Select(pr => new PurchaseRequisitionSummary
                {
                    PrId = pr.PrId,
                    PrNumber = pr.PrNumber,
                    RequestDate = pr.RequestDate,
                    Description = pr.Description,
                    CreatedBy =
                        pr.CreatedByUser != null
                            ? pr.CreatedByUser.FullName ?? string.Empty
                            : string.Empty,
                    CreatedAt = pr.CreatedAt,
                    ProcurementCount = pr.Procurements.Count,
                })
                .ToListAsync(ct);
        }

        public async Task<int> GetTotalPurchaseRequisitionsCountAsync(
            CancellationToken ct = default
        )
        {
            return await _context.PurchaseRequisitions.CountAsync(ct);
        }

        // Trends and Charts
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
                    TotalValue = 0m, // PR doesn't have direct value, set to 0
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
            // ProcDocumentApprovals removed - sekarang approval stats dari PR status
            return await _context
                .PurchaseRequisitions
                .Where(pr => pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager
                    || pr.Status == PurchaseRequisitionStatus.DonePO)
                .GroupBy(pr => pr.Status.ToString())
                .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        }

        // User Activity
        public async Task<List<RecentLoginSummary>> GetUserActivityStatusAsync(
            int take = 30,
            CancellationToken ct = default
        )
        {
            var onlineThreshold = DateTime.Now.AddMinutes(-15);

            return await _context
                .Users.Where(u => u.LastLoginAt != null && u.IsActive)
                .OrderByDescending(u => u.LastLoginAt)
                .Take(take)
                .Select(u => new RecentLoginSummary
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? string.Empty,
                    UserName = u.UserName ?? string.Empty,
                    JobTitle = u.JobTitle,
                    LastLoginAt = u.LastLoginAt!.Value,
                    IsOnline = u.LastLoginAt >= onlineThreshold,
                })
                .ToListAsync(ct);
        }
    }
}
