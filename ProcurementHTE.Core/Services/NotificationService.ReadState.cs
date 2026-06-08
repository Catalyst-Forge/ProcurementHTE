using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class NotificationService
    {
        public async Task<NotificationListResponse> GetNotificationsAsync(
            string userId,
            int skip = 0,
            int take = 20,
            bool unreadOnly = false,
            CancellationToken ct = default
        )
        {
            var notifications = await _repository.GetByUserIdAsync(
                userId,
                skip,
                take,
                unreadOnly,
                ct
            );
            var unreadCount = await _repository.GetUnreadCountAsync(userId, ct);

            return new NotificationListResponse
            {
                Notifications = notifications,
                UnreadCount = unreadCount,
                TotalCount = notifications.Count,
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        {
            return await _repository.GetUnreadCountAsync(userId, ct);
        }

        public async Task MarkAsReadAsync(string notificationId, CancellationToken ct = default)
        {
            await _repository.MarkAsReadAsync(notificationId, ct);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        {
            await _repository.MarkAllAsReadAsync(userId, ct);
        }
    }
}
