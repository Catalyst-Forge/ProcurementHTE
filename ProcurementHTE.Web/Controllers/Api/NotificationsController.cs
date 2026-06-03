using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<User> userManager
        )
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] bool unreadOnly = false,
            CancellationToken ct = default
        )
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _notificationService.GetNotificationsAsync(
                userId,
                skip,
                take,
                unreadOnly,
                ct
            );

            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId, ct);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(
            string notificationId,
            CancellationToken ct = default
        )
        {
            await _notificationService.MarkAsReadAsync(notificationId, ct);

            var userId = _userManager.GetUserId(User);
            var unreadCount = !string.IsNullOrEmpty(userId)
                ? await _notificationService.GetUnreadCountAsync(userId, ct)
                : 0;

            return Ok(new { success = true, unreadCount });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId, ct);
            return Ok(new { success = true, unreadCount = 0 });
        }
    }
}
