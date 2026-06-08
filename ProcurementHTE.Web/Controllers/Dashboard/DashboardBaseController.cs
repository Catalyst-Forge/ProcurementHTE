using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Mappers;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize]
    public abstract partial class DashboardBaseController : Controller
    {
        protected readonly IProcurementQueryService _procurementQueryService;
        protected readonly UserManager<User> UserManager;
        protected readonly IProfitLossService ProfitLossService;
        protected readonly IDashboardService DashboardService;

        protected DashboardBaseController(
            IProcurementQueryService procurementQueryService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService
        )
        {
            _procurementQueryService = procurementQueryService;
            UserManager = userManager;
            ProfitLossService = profitLossService;
            DashboardService = dashboardService;
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

            var recentProcurements = await _procurementQueryService.GetMyRecentProcurementAsync(
                userId,
                5,
                ct
            );
            var totalRevenueThisMonth = await ProfitLossService.GetTotalRevenueThisMonthAsync();
            var procurementsByStatus = await DashboardService.GetProcurementStatusCountsAsync();
            var revenuePerMonth = await DashboardService.GetRevenuePerMonthAsync(DateTime.Now.Year);
            // approvalStatus removed - approval per-document sudah dihapus

            // Fetch DTOs from service layer
            var recentProcsDto = await DashboardService.GetRecentProcurementsAsync(10, ct);
            var pendingApprovalsDto = await DashboardService.GetPendingApprovalsDetailAsync(10, ct);
            var jobTypeDistDto = await DashboardService.GetJobTypeDistributionAsync(ct);
            var topVendorsDto = await DashboardService.GetTopVendorsAsync(10, ct);
            var monthlyTrendDto = await DashboardService.GetMonthlyProcurementTrendAsync(ct);
            var monthlyPrTrendDto = await DashboardService.GetMonthlyPurchaseRequisitionTrendAsync(
                ct
            );
            var procsByStatusDto = await DashboardService.GetProcurementsByStatusAsync(ct);
            var docApprovalStatsDto = await DashboardService.GetDocumentApprovalStatsAsync(ct);
            var recentPurchaseReqsDto = await DashboardService.GetRecentPurchaseRequisitionsAsync(
                10,
                ct
            );

            var model = new DashboardViewModel
            {
                RoleName = roleName,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                TotalProcurements = await _procurementQueryService.CountAllProcurementsAsync(ct),

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

                // Status Counts - DTOs only for chart data
                ProcurementStatusCounts = procurementsByStatus,
                RevenuePerMonth = revenuePerMonth,
                // ApprovalStatus removed - approval per-document sudah dihapus

                // Core Metrics
                ActiveProcurements = await DashboardService.GetActiveProcurementsCountAsync(ct),
                PendingApprovals = await DashboardService.GetPendingApprovalsCountAsync(ct),
                TotalVendors = await DashboardService.GetTotalVendorsCountAsync(ct),

                // Financial Data
                TotalRevenue = await DashboardService.GetTotalRevenueAsync(ct),
                TotalCost = await DashboardService.GetTotalCostAsync(ct),
                TotalProfit = await DashboardService.GetTotalProfitAsync(ct),

                // Trends and Lists - Convert DTOs to ViewModels using Mapper
                MonthlyProcurementTrend = monthlyTrendDto.ToViewModelList(),
                MonthlyPurchaseRequisitionTrend = monthlyPrTrendDto.ToViewModelList(),
                RecentProcurements = recentProcsDto.ToViewModelList(),
                PendingApprovalsDetail = pendingApprovalsDto.ToViewModelList(),
                JobTypeDistribution = jobTypeDistDto.ToViewModelList(),
                TopVendors = topVendorsDto.ToViewModelList(),
                ProcurementsByStatus = procsByStatusDto.ToViewModelList(),
                DocumentApprovalStats = docApprovalStatsDto.ToViewModelList(),
                RecentPurchaseRequisitions = recentPurchaseReqsDto.ToViewModelList(),
                TotalPurchaseRequisitions =
                    await DashboardService.GetTotalPurchaseRequisitionsCountAsync(ct),
            };

            // Admin-only: Add Recent Activities and User Activity Status
            if (isAdmin)
            {
                var activitiesDto = await DashboardService.GetRecentActivitiesAsync(10);
                model.RecentActivities = activitiesDto.ToViewModelList();

                var userActivityDto = await DashboardService.GetUserActivityStatusAsync(30, ct);
                model.UserActivities = userActivityDto.ToViewModelList();
                model.OnlineUsersCount = userActivityDto.Count(u => u.IsOnline);

                // Accrual Statistics
                var accrualStats = await DashboardService.GetAccrualStatisticsAsync(ct);
                model.PendingAccrualCount = accrualStats.PendingCount;
                model.FilledAccrualCount = accrualStats.FilledCount;
                model.TotalPotensiAccrual = accrualStats.TotalPotensiAccrual;

                // Role Distribution
                var roleDistribution = await GetRoleDistributionAsync();
                model.RoleDistribution = roleDistribution;

                // Region Distribution
                var regionDistribution = await DashboardService.GetRegionDistributionAsync(ct);
                model.RegionDistribution = regionDistribution
                    .Select(r => new RegionDistributionViewModel
                    {
                        RegionName = r.RegionName,
                        Count = r.Count,
                        TotalValue = r.TotalValue
                    })
                    .ToList();
            }

            return model;
        }

        protected async Task<DashboardViewModel> BuildFullDashboardAsync(
            CancellationToken ct = default
        )
        {
            // Fetch DTOs from service layer
            var procsByStatusDto = await DashboardService.GetProcurementsByStatusAsync(ct);
            var monthlyTrendDto = await DashboardService.GetMonthlyProcurementTrendAsync(ct);
            var monthlyPrTrendDto = await DashboardService.GetMonthlyPurchaseRequisitionTrendAsync(
                ct
            );
            var recentProcsDto = await DashboardService.GetRecentProcurementsAsync(10, ct);
            var pendingApprovalsDto = await DashboardService.GetPendingApprovalsDetailAsync(10, ct);
            var jobTypeDistDto = await DashboardService.GetJobTypeDistributionAsync(ct);
            var topVendorsDto = await DashboardService.GetTopVendorsAsync(10, ct);
            var docApprovalStatsDto = await DashboardService.GetDocumentApprovalStatsAsync(ct);
            var recentPurchaseReqsDto = await DashboardService.GetRecentPurchaseRequisitionsAsync(
                10,
                ct
            );

            return new DashboardViewModel
            {
                TotalProcurements = await _procurementQueryService.CountAllProcurementsAsync(ct),
                ActiveProcurements = await DashboardService.GetActiveProcurementsCountAsync(ct),
                PendingApprovals = await DashboardService.GetPendingApprovalsCountAsync(ct),
                TotalVendors = await DashboardService.GetTotalVendorsCountAsync(ct),

                TotalRevenue = await DashboardService.GetTotalRevenueAsync(ct),
                TotalCost = await DashboardService.GetTotalCostAsync(ct),
                TotalProfit = await DashboardService.GetTotalProfitAsync(ct),

                // Convert DTOs to ViewModels using Mapper
                ProcurementsByStatus = procsByStatusDto.ToViewModelList(),
                MonthlyProcurementTrend = monthlyTrendDto.ToViewModelList(),
                MonthlyPurchaseRequisitionTrend = monthlyPrTrendDto.ToViewModelList(),
                RecentProcurements = recentProcsDto.ToViewModelList(),
                PendingApprovalsDetail = pendingApprovalsDto.ToViewModelList(),
                JobTypeDistribution = jobTypeDistDto.ToViewModelList(),
                TopVendors = topVendorsDto.ToViewModelList(),
                DocumentApprovalStats = docApprovalStatsDto.ToViewModelList(),
                RecentPurchaseRequisitions = recentPurchaseReqsDto.ToViewModelList(),
                TotalPurchaseRequisitions =
                    await DashboardService.GetTotalPurchaseRequisitionsCountAsync(ct),
            };
        }
    }
}
