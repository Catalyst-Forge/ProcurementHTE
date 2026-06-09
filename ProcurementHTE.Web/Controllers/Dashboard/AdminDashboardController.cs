using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Hubs;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize(Roles = DashboardRoleHelper.AdminRole)]
    [Route("Dashboard/Admin")]
    public class AdminDashboardController : DashboardBaseController
    {
        public AdminDashboardController(
            IProcurementQueryService procurementQueryService,
            UserManager<User> userManager,
            IProfitLossQueryService profitLossService,
            IDashboardService dashboardService
        )
            : base(procurementQueryService, userManager, profitLossService, dashboardService) { }

        [HttpGet("")]
        public Task<IActionResult> Index(CancellationToken ct = default) =>
            RenderDashboardAsync(
                "~/Views/Dashboard/Admin.cshtml",
                DashboardRoleHelper.AdminRole,
                ct
            );

        [HttpGet("GetOnlineUsers")]
        public IActionResult GetOnlineUsers()
        {
            // Get all users from database
            var allUsers = UserManager
                .Users.OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    FullName = u.FirstName + " " + u.LastName,
                    u.LastLoginAt,
                    u.IsActive,
                })
                .ToList();

            // Get online users from SignalR Hub
            var onlineUsers = DashboardHub.GetOnlineUsers();
            var onlineUserIds = onlineUsers.Select(u => u.UserId).ToHashSet();

            // Combine data
            var userStatuses = allUsers
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.FullName,
                    u.LastLoginAt,
                    u.IsActive,
                    IsOnline = onlineUserIds.Contains(u.Id),
                    ConnectionInfo = onlineUsers.FirstOrDefault(ou => ou.UserId == u.Id),
                })
                .ToList();

            return Ok(
                new
                {
                    TotalUsers = allUsers.Count,
                    OnlineCount = onlineUserIds.Count,
                    OfflineCount = allUsers.Count - onlineUserIds.Count,
                    Users = userStatuses,
                }
            );
        }
    }
}
