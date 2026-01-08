using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.Dashboard
{
    [Authorize]
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public DashboardController(
            UserManager<User> userManager,
            INotificationService notificationService
        )
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet("")]
        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (DashboardRoleHelper.TryGetControllerForRole(role, out var controller))
                {
                    return RedirectToAction("Index", controller);
                }
            }

            return View("~/Views/Dashboard/UnknownRole.cshtml");
        }

        [HttpGet("UnknownRole")]
        public IActionResult UnknownRole()
        {
            return View("~/Views/Dashboard/UnknownRole.cshtml");
        }

        [HttpGet("Notifications")]
        public async Task<IActionResult> Notifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false,
            CancellationToken ct = default
        )
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var skip = (page - 1) * pageSize;
            var result = await _notificationService.GetNotificationsAsync(
                userId,
                skip,
                pageSize + 1,
                unreadOnly,
                ct
            );

            var notifications = result.Notifications.Take(pageSize).ToList();
            var hasMore = result.Notifications.Count > pageSize;

            ViewBag.CurrentPage = page;
            ViewBag.HasNextPage = hasMore;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.UnreadCount = result.UnreadCount;

            return View("~/Views/Dashboard/Notifications.cshtml", notifications);
        }
    }
}
