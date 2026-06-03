using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Services
{
    public class NotificationPusher<THub> : INotificationPusher
        where THub : Hub
    {
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<NotificationPusher<THub>> _logger;

        public NotificationPusher(
            IHubContext<THub> hubContext,
            ILogger<NotificationPusher<THub>> logger
        )
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task PushToUserAsync(string userId, PushNotificationDto notification)
        {
            try
            {
                // Send to user-specific group (user_{userId})
                await _hubContext
                    .Clients.Group($"user_{userId}")
                    .SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            notificationId = notification.NotificationId,
                            title = notification.Title,
                            message = notification.Message,
                            notificationType = notification.NotificationType,
                            actionUrl = notification.ActionUrl,
                            createdAt = notification.CreatedAt,
                            unreadCount = notification.UnreadCount,
                        }
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to push notification {NotificationId} to user {UserId}",
                    notification.NotificationId,
                    userId
                );
            }
        }

        public async Task PushToUsersAsync(
            IEnumerable<string> userIds,
            PushNotificationDto notification
        )
        {
            var tasks = userIds.Select(userId => PushToUserAsync(userId, notification));
            await Task.WhenAll(tasks);
        }

        public async Task BroadcastAsync(PushNotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notificationId = notification.NotificationId,
                        title = notification.Title,
                        message = notification.Message,
                        notificationType = notification.NotificationType,
                        actionUrl = notification.ActionUrl,
                        createdAt = notification.CreatedAt,
                        unreadCount = notification.UnreadCount,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to broadcast notification {NotificationId}",
                    notification.NotificationId
                );
            }
        }

        public async Task UpdateBadgeCountAsync(string userId, int unreadCount)
        {
            try
            {
                await _hubContext
                    .Clients.Group($"user_{userId}")
                    .SendAsync("UpdateNotificationBadge", new { unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update notification badge for user {UserId}",
                    userId
                );
            }
        }

        public async Task PushApprovalBadgeAsync(string userId, int pendingCount)
        {
            try
            {
                await _hubContext
                    .Clients.Group($"user_{userId}")
                    .SendAsync("UpdateApprovalBadge", new { pendingCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update approval badge for user {UserId}",
                    userId
                );
            }
        }
    }
}
