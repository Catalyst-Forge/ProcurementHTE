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
        private readonly IWorkOrderService _woService;
        private readonly UserManager<User> _userManager;
        private readonly IProfitLossService _pnlService;
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IWorkOrderService woService,
            UserManager<User> userManager,
            IProfitLossService pnlService,
            IDashboardService dashboardService
        )
        {
            _woService = woService;
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
            var recentWo = await _woService.GetMyRecentWorkOrderAsync(userId, 5, ct);
            var totalRevenueThisMonth = await _pnlService.GetTotalRevenueThisMonthAsync();
            var activities = await _dashboardService.GetRecentActivitiesAsync(5);
            var woByStatus = await _dashboardService.GetWoStatusCountsAsync();
            var revenuePerMonth = await _dashboardService.GetRevenuePerMonthAsync(
                DateTime.Now.Year
            );
            var approvalStatus = await _dashboardService.GetApprovalStatusCountsAsync();

            ViewBag.TotalWo = await _woService.CountAllWoAsync(ct);
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.TotalRevenueThisMonth = totalRevenueThisMonth;
            ViewBag.RecentActivities = activities;
            ViewBag.WoByStatus = woByStatus;
            ViewBag.RevenuePerMonth = revenuePerMonth;
            ViewBag.ApprovalStatus = approvalStatus;

            var woViewModel = recentWo.Select(item => new DashboardWoViewModel
            {
                WoNum = item.WoNum!,
                Description = item.Description,
                ProcurementType = item.ProcurementType,
                StatusName = item.Status?.StatusName!,
                CreatedAt = item.CreatedAt,
            });

            return View(woViewModel);
        }
    }
}
