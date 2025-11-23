using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
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

        protected DashboardBaseController(
            IProcurementService procurementService,
            UserManager<User> userManager,
            IProfitLossService profitLossService,
            IDashboardService dashboardService)
        {
            ProcurementService = procurementService;
            UserManager = userManager;
            ProfitLossService = profitLossService;
            DashboardService = dashboardService;
        }

        protected async Task<DashboardSummaryViewModel> BuildDashboardAsync(
            User user,
            string roleName,
            CancellationToken ct = default)
        {
            var userId = user.Id;

            var totalUsers = UserManager.Users.Count();
            var activeUsers = UserManager.Users.Count(u => u.IsActive);

            var recentProcurements = await ProcurementService.GetMyRecentProcurementAsync(userId, 5, ct);
            var totalRevenueThisMonth = await ProfitLossService.GetTotalRevenueThisMonthAsync();
            var activities = await DashboardService.GetRecentActivitiesAsync(5);
            var procurementsByStatus = await DashboardService.GetProcurementStatusCountsAsync();
            var revenuePerMonth = await DashboardService.GetRevenuePerMonthAsync(DateTime.Now.Year);
            var approvalStatus = await DashboardService.GetApprovalStatusCountsAsync();

            return new DashboardSummaryViewModel
            {
                RoleName = roleName,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                TotalProcurements = await ProcurementService.CountAllProcurementsAsync(ct),
                RecentProcurements = recentProcurements
                    .Select(item => new DashboardProcurementViewModel
                    {
                        ProcNum = item.ProcNum ?? "-",
                        JobName = item.JobName ?? item.Note,
                        JobTypeName = item.JobType?.TypeName,
                        StatusName = item.Status?.StatusName ?? "-",
                        CreatedAt = item.CreatedAt,
                    })
                    .ToList(),
                RecentActivities = activities,
                ProcurementsByStatus = procurementsByStatus,
                RevenuePerMonth = revenuePerMonth,
                ApprovalStatus = approvalStatus,
            };
        }

        protected async Task<IActionResult> RenderDashboardAsync(
            string viewPath,
            string roleName,
            CancellationToken ct = default)
        {
            var user = await UserManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var model = await BuildDashboardAsync(user, roleName, ct);
            return View(viewPath, model);
        }
    }
}
