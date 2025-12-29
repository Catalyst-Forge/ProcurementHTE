using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize]
    public abstract class DashboardBaseController : Controller
    {
        protected readonly IProcurementService ProcurementService;
        protected readonly UserManager<User> UserManager;
        protected readonly IProfitLossService ProfitLossService;
        protected readonly IDashboardService DashboardService;
        protected readonly AppDbContext Context;

        protected DashboardBaseController(
            IProcurementService procurementService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService,
            AppDbContext context
        )
        {
            ProcurementService = procurementService;
            UserManager = userManager;
            ProfitLossService = profitLossService;
            DashboardService = dashboardService;
            Context = context;
        }

        protected async Task<DashboardViewModel> BuildDashboardAsync(
            User user,
            string roleName,
            CancellationToken ct = default
        )
        {
            var userId = user.Id;
            var isAdmin = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            var totalUsers = UserManager.Users.Count();
            var activeUsers = UserManager.Users.Count(u => u.IsActive);

            var recentProcurements = await ProcurementService.GetMyRecentProcurementAsync(
                userId,
                5,
                ct
            );
            var totalRevenueThisMonth = await ProfitLossService.GetTotalRevenueThisMonthAsync();
            var procurementsByStatus = await DashboardService.GetProcurementStatusCountsAsync();
            var revenuePerMonth = await DashboardService.GetRevenuePerMonthAsync(DateTime.Now.Year);
            var approvalStatus = await DashboardService.GetApprovalStatusCountsAsync();

            var model = new DashboardViewModel
            {
                RoleName = roleName,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                TotalProcurements = await ProcurementService.CountAllProcurementsAsync(ct),

                // Recent Procurements - using unified list
                RecentProcurementsList = recentProcurements
                    .Select(item => new DashboardProcurementViewModel
                    {
                        ProcNum = item.ProcNum ?? "-",
                        JobName = item.JobName ?? item.Note,
                        JobTypeName = item.JobType?.TypeName,
                        StatusName = item.Status?.StatusName ?? "-",
                        CreatedAt = item.CreatedAt,
                    })
                    .ToList(),

                // Status Counts
                ProcurementStatusCounts = procurementsByStatus,
                RevenuePerMonth = revenuePerMonth,
                ApprovalStatus = approvalStatus,

                // Core Metrics
                ActiveProcurements = await Context
                    .Procurements.Where(p => p.Status != null)
                    .CountAsync(
                        p =>
                            p.Status!.StatusName == "Created"
                            || p.Status!.StatusName == "In Progress",
                        ct
                    ),
                PendingApprovals = await Context.ProcDocumentApprovals.CountAsync(
                    a => a.Status == "Pending",
                    ct
                ),
                TotalVendors = await Context.Vendors.CountAsync(ct),

                // Financial Data
                TotalRevenue =
                    await Context.ProfitLossItems.SumAsync(pnl => (decimal?)pnl.Revenue, ct) ?? 0m,
                TotalCost =
                    await Context.ProfitLosses.SumAsync(
                        pnl => (decimal?)pnl.SelectedVendorFinalOffer,
                        ct
                    ) ?? 0m,
                TotalProfit =
                    await Context.ProfitLosses.SumAsync(pnl => (decimal?)pnl.Profit, ct) ?? 0m,

                MonthlyProcurementTrend = await GetMonthlyProcurementTrendAsync(ct),

                // Detailed Lists
                RecentProcurements = await Context
                    .Procurements.Include(proc => proc.Status)
                    .Include(proc => proc.JobType)
                    .Include(proc => proc.User)
                    .Include(proc => proc.ProfitLosses)
                    .ThenInclude(pl => pl.Items)
                    .OrderByDescending(proc => proc.CreatedAt)
                    .Take(10)
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
                    .ToListAsync(ct),

                PendingApprovalsDetail = await Context
                    .ProcDocumentApprovals.Include(a => a.ProcDocument)
                    .ThenInclude(d => d.Procurement)
                    .Include(a => a.Role)
                    .Where(a => a.Status == "Pending")
                    .OrderByDescending(a => a.ProcDocument.CreatedAt)
                    .Take(10)
                    .Select(a => new ApprovalSummary
                    {
                        ProcNum = a.ProcDocument.Procurement.ProcNum,
                        DocumentName = a.ProcDocument.FileName,
                        ApprovalRole = a.Role != null ? a.Role.Name! : string.Empty,
                        CreatedDate = a.ProcDocument.CreatedAt,
                    })
                    .ToListAsync(ct),

                JobTypeDistribution = await Context
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
                    .ToListAsync(ct),

                TopVendors = await Context
                    .Vendors.Select(v => new VendorPerformance
                    {
                        VendorCode = v.VendorCode,
                        VendorName = v.VendorName,
                        OfferCount = Context.VendorOffers.Count(vo => vo.VendorId == v.VendorId),
                        SelectedCount = Context.ProfitLosses.Count(pl =>
                            pl.SelectedVendorId == v.VendorId
                        ),
                    })
                    .Where(v => v.OfferCount > 0)
                    .OrderByDescending(v => v.OfferCount)
                    .Take(10)
                    .ToListAsync(ct),

                TotalPurchaseRequisitions = await Context.PurchaseRequisitions.CountAsync(ct),

                RecentPurchaseRequisitions = await Context
                    .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
                    .Include(pr => pr.Procurements)
                    .OrderByDescending(pr => pr.CreatedAt)
                    .Take(10)
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
                    .ToListAsync(ct),

                ProcurementsByStatus = await Context
                    .Procurements.Where(proc => proc.Status != null)
                    .GroupBy(proc => proc.Status!.StatusName)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(ct),

                DocumentApprovalStats = await Context
                    .ProcDocumentApprovals.GroupBy(a => a.Status)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(ct),
            };

            // Admin-only: Add Recent Activities for monitoring
            if (isAdmin)
            {
                var activities = await DashboardService.GetRecentActivitiesAsync(10);
                model.RecentActivities = activities;
            }

            return model;
        }

        protected async Task<IActionResult> RenderDashboardAsync(
            string viewPath,
            string roleName,
            CancellationToken ct = default
        )
        {
            var user = await UserManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var model = await BuildDashboardAsync(user, roleName, ct);
            return View(viewPath, model);
        }

        protected async Task<DashboardViewModel> BuildFullDashboardAsync(
            CancellationToken ct = default
        )
        {
            return new DashboardViewModel
            {
                TotalProcurements = await Context.Procurements.CountAsync(ct),
                ActiveProcurements = await Context
                    .Procurements.Where(p => p.Status != null)
                    .CountAsync(
                        p =>
                            p.Status!.StatusName == "Created"
                            || p.Status!.StatusName == "In Progress",
                        ct
                    ),
                PendingApprovals = await Context.ProcDocumentApprovals.CountAsync(
                    a => a.Status == "Pending",
                    ct
                ),
                TotalVendors = await Context.Vendors.CountAsync(ct),

                TotalRevenue =
                    await Context.ProfitLossItems.SumAsync(pnl => (decimal?)pnl.Revenue, ct) ?? 0m,
                TotalCost =
                    await Context.ProfitLosses.SumAsync(
                        pnl => (decimal?)pnl.SelectedVendorFinalOffer,
                        ct
                    ) ?? 0m,
                TotalProfit =
                    await Context.ProfitLosses.SumAsync(pnl => (decimal?)pnl.Profit, ct) ?? 0m,

                ProcurementsByStatus = await Context
                    .Procurements.Where(proc => proc.Status != null)
                    .GroupBy(proc => proc.Status!.StatusName)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(ct),

                MonthlyProcurementTrend = await GetMonthlyProcurementTrendAsync(ct),

                RecentProcurements = await Context
                    .Procurements.Include(proc => proc.Status)
                    .Include(proc => proc.JobType)
                    .Include(proc => proc.User)
                    .Include(proc => proc.ProfitLosses)
                    .ThenInclude(pl => pl.Items)
                    .OrderByDescending(proc => proc.CreatedAt)
                    .Take(10)
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
                    .ToListAsync(ct),

                PendingApprovalsDetail = await Context
                    .ProcDocumentApprovals.Include(a => a.ProcDocument)
                    .ThenInclude(d => d.Procurement)
                    .Include(a => a.Role)
                    .Where(a => a.Status == "Pending")
                    .OrderByDescending(a => a.ProcDocument.CreatedAt)
                    .Take(10)
                    .Select(a => new ApprovalSummary
                    {
                        ProcNum = a.ProcDocument.Procurement.ProcNum,
                        DocumentName = a.ProcDocument.FileName,
                        ApprovalRole = a.Role != null ? a.Role.Name! : string.Empty,
                        CreatedDate = a.ProcDocument.CreatedAt,
                    })
                    .ToListAsync(ct),

                JobTypeDistribution = await Context
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
                    .ToListAsync(ct),

                TopVendors = await Context
                    .Vendors.Select(v => new VendorPerformance
                    {
                        VendorCode = v.VendorCode,
                        VendorName = v.VendorName,
                        OfferCount = Context.VendorOffers.Count(vo => vo.VendorId == v.VendorId),
                        SelectedCount = Context.ProfitLosses.Count(pl =>
                            pl.SelectedVendorId == v.VendorId
                        ),
                    })
                    .Where(v => v.OfferCount > 0)
                    .OrderByDescending(v => v.OfferCount)
                    .Take(10)
                    .ToListAsync(ct),

                DocumentApprovalStats = await Context
                    .ProcDocumentApprovals.GroupBy(a => a.Status)
                    .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                    .ToListAsync(ct),

                TotalPurchaseRequisitions = await Context.PurchaseRequisitions.CountAsync(ct),

                RecentPurchaseRequisitions = await Context
                    .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
                    .Include(pr => pr.Procurements)
                    .OrderByDescending(pr => pr.CreatedAt)
                    .Take(10)
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
                    .ToListAsync(ct),
            };
        }

        protected async Task<List<MonthlyTrend>> GetMonthlyProcurementTrendAsync(
            CancellationToken ct = default
        )
        {
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);

            var monthlyData = await Context
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

            return monthlyData;
        }

        protected async Task<List<StatusCount>> GetProcurementStatusChartDataAsync(
            CancellationToken ct = default
        )
        {
            return await Context
                .Procurements.Where(p => p.Status != null)
                .GroupBy(p => p.Status!.StatusName)
                .Select(g => new StatusCount { StatusName = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        }
    }
}
