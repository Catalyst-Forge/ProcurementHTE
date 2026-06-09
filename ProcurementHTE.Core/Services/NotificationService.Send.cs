using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class NotificationService
    {
        public async Task SendNotificationAsync(
            string userId,
            string title,
            string message,
            string notificationType,
            string? actionUrl = null,
            string? referenceId = null,
            string? createdByUserId = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    ActionUrl = actionUrl,
                    ReferenceId = referenceId,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                };

                await _repository.CreateAsync(notification, ct);
                var unreadCount = await _repository.GetUnreadCountAsync(userId, ct);
                var pushDto = new PushNotificationDto
                {
                    NotificationId = notification.NotificationId,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    ActionUrl = actionUrl,
                    CreatedAt = notification.CreatedAt,
                    UnreadCount = unreadCount,
                };

                await _pusher.PushToUserAsync(userId, pushDto);
                _logger.LogInformation(
                    "Notification sent to user {UserId}: {Title}",
                    userId,
                    title
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        public async Task SendNotificationToRoleAsync(
            string roleName,
            string title,
            string message,
            string notificationType,
            string? actionUrl = null,
            string? referenceId = null,
            string? createdByUserId = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var usersInRole = await _userRepository.GetUsersByRoleAsync(roleName, ct);
                if (!usersInRole.Any())
                {
                    _logger.LogWarning("No users found with role {RoleName}", roleName);
                    return;
                }

                var notifications = usersInRole
                    .Select(user => new Notification
                    {
                        UserId = user.UserId,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        ActionUrl = actionUrl,
                        ReferenceId = referenceId,
                        CreatedByUserId = createdByUserId,
                        CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                    })
                    .ToList();

                await _repository.CreateManyAsync(notifications, ct);

                foreach (var notification in notifications)
                {
                    var unreadCount = await _repository.GetUnreadCountAsync(
                        notification.UserId,
                        ct
                    );
                    var pushDto = new PushNotificationDto
                    {
                        NotificationId = notification.NotificationId,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        ActionUrl = actionUrl,
                        CreatedAt = notification.CreatedAt,
                        UnreadCount = unreadCount,
                    };

                    await _pusher.PushToUserAsync(notification.UserId, pushDto);
                }

                _logger.LogInformation(
                    "Notification sent to {Count} users with role {RoleName}: {Title}",
                    usersInRole.Count,
                    roleName,
                    title
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to role {RoleName}", roleName);
            }
        }
    }
}
