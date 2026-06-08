using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalsController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ApprovalsController> _logger;

        public ApprovalsController(
            IDashboardService dashboardService,
            UserManager<User> userManager,
            ILogger<ApprovalsController> logger
        )
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get pending approval count for the current user based on their role and assigned procurements
        /// </summary>
        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount(CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetPendingCount: userId is null or empty");
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("GetPendingCount: user not found for userId {UserId}", userId);
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var rolesArray = roles.ToArray();

            _logger.LogInformation("GetPendingCount: User {UserName} (ID: {UserId}) with roles [{Roles}]",
                user.UserName, userId, string.Join(", ", rolesArray));

            // Check if user has approver role
            var approverRoles = new[] { "Admin", "Analyst HTE & LTS", "Assistant Manager HTE", "Manager Transport & Logistic" };
            if (!rolesArray.Any(r => approverRoles.Contains(r)))
            {
                _logger.LogInformation("GetPendingCount: User {UserName} has no approver role, returning 0", user.UserName);
                return Ok(new { pendingCount = 0 });
            }

            var count = await _dashboardService.GetPendingApprovalCountByUserAsync(userId, rolesArray, ct);
            _logger.LogInformation("GetPendingCount: User {UserName} has {Count} pending approvals", user.UserName, count);
            return Ok(new { pendingCount = count });
        }

        /// <summary>
        /// Get pending approvals list for the current user based on their role and assigned procurements
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 15,
            CancellationToken ct = default
        )
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var rolesArray = roles.ToArray();

            // Check if user has approver role
            var approverRoles = new[] { "Admin", "Analyst HTE & LTS", "Assistant Manager HTE", "Manager Transport & Logistic" };
            if (!rolesArray.Any(r => approverRoles.Contains(r)))
            {
                return Ok(new { items = Array.Empty<object>(), totalCount = 0 });
            }

            var (items, totalCount) = await _dashboardService.GetPendingApprovalsByUserAsync(
                userId,
                rolesArray,
                skip,
                take,
                ct
            );

            return Ok(new
            {
                items = items.Select(i => new
                {
                    procurementId = i.ProcurementId,
                    procNum = i.ProcNum,
                    wonum = i.Wonum,
                    jobName = i.JobName,
                    currentStatus = i.CurrentStatus,
                    currentStatusDescription = i.CurrentStatusDescription,
                    documentDate = i.DocumentDate,
                    sentForApprovalAt = i.SentForApprovalAt
                }),
                totalCount
            });
        }
    }
}
