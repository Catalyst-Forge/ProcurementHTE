using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IProcurementService _procurementService;
        private readonly UserManager<User> _userManager;
        private readonly IProfitLossService _pnlService;
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IProcurementService procurementService,
            UserManager<User> userManager,
            IProfitLossService pnlService,
            IDashboardService dashboardService
        )
        {
            _procurementService = procurementService;
            _userManager = userManager;
            _pnlService = pnlService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var totalUsers = _userManager.Users.Count();
            var activeUsers = _userManager.Users.Count(user => user.IsActive);
            var recentProcurements = await _procurementService.GetMyRecentProcurementAsync(userId, 5, ct);
            var totalRevenueThisMonth = await _pnlService.GetTotalRevenueThisMonthAsync();
            var activities = await _dashboardService.GetRecentActivitiesAsync(5);
            var procurementsByStatus = await _dashboardService.GetProcurementStatusCountsAsync();
            var revenuePerMonth = await _dashboardService.GetRevenuePerMonthAsync(
                DateTime.Now.Year
            );
            var approvalStatus = await _dashboardService.GetApprovalStatusCountsAsync();

            ViewBag.TotalProcurements = await _procurementService.CountAllProcurementsAsync(ct);
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.TotalRevenueThisMonth = totalRevenueThisMonth;
            ViewBag.RecentActivities = activities;
            ViewBag.ProcurementsByStatus = procurementsByStatus;
            ViewBag.RevenuePerMonth = revenuePerMonth;
            ViewBag.ApprovalStatus = approvalStatus;

            var procurementViewModels = recentProcurements.Select(item => new DashboardProcurementViewModel
            {
                ProcNum = item.ProcNum ?? "-",
                JobName = item.JobName ?? item.Note,
                JobTypeName = item.JobType?.TypeName,
                StatusName = item.Status?.StatusName ?? "-",
                CreatedAt = item.CreatedAt,
            });

            return View(procurementViewModels);
        }
    }
}
