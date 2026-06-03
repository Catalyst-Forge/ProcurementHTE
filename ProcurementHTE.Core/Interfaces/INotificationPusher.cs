using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface INotificationPusher
    {
        Task PushToUserAsync(string userId, PushNotificationDto notification);
        Task PushToUsersAsync(IEnumerable<string> userIds, PushNotificationDto notification);
        Task BroadcastAsync(PushNotificationDto notification);
        Task UpdateBadgeCountAsync(string userId, int unreadCount);
        
        /// <summary>
        /// Push pending approval badge count update to a specific user
        /// </summary>
        Task PushApprovalBadgeAsync(string userId, int pendingCount);
    }
}
