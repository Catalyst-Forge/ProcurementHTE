using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface INotificationRepository
    {
        Task CreateAsync(Notification notification, CancellationToken ct = default);
        Task CreateManyAsync(
            IEnumerable<Notification> notifications,
            CancellationToken ct = default
        );
        Task<Notification?> GetByIdAsync(string notificationId, CancellationToken ct = default);
        Task<List<NotificationDto>> GetByUserIdAsync(
            string userId,
            int skip = 0,
            int take = 20,
            bool unreadOnly = false,
            CancellationToken ct = default
        );
        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
        Task MarkAsReadAsync(string notificationId, CancellationToken ct = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
        Task DeleteAsync(string notificationId, CancellationToken ct = default);
        Task DeleteOldNotificationsAsync(int daysOld = 30, CancellationToken ct = default);
    }
}
