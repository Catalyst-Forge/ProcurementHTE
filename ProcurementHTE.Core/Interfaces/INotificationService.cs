using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(
            string userId,
            string title,
            string message,
            string notificationType,
            string? actionUrl = null,
            string? referenceId = null,
            string? createdByUserId = null,
            CancellationToken ct = default
        );

        Task SendNotificationToRoleAsync(
            string roleName,
            string title,
            string message,
            string notificationType,
            string? actionUrl = null,
            string? referenceId = null,
            string? createdByUserId = null,
            CancellationToken ct = default
        );

        Task NotifyProcurementPublishedAsync(
            string procurementId,
            string procNum,
            string publishedByUserId,
            string publishedByUserName,
            CancellationToken ct = default
        );

        Task NotifyDocumentApprovedAsync(
            string prId,
            string prNumber,
            string approverRole,
            string approverUserId,
            string approverUserName,
            string nextApproverRole,
            CancellationToken ct = default
        );

        Task NotifyPrCompletedAsync(
            string prId,
            string prNumber,
            string appoUserId,
            CancellationToken ct = default
        );

        Task NotifyPrRejectedAsync(
            string prId,
            string prNumber,
            string rejectedByUserId,
            string rejectedByUserName,
            string rejectionNote,
            string appoUserId,
            CancellationToken ct = default
        );

        Task<NotificationListResponse> GetNotificationsAsync(
            string userId,
            int skip = 0,
            int take = 20,
            bool unreadOnly = false,
            CancellationToken ct = default
        );

        Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

        Task MarkAsReadAsync(string notificationId, CancellationToken ct = default);

        Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    }
}
