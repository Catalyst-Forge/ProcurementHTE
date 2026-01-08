using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationPusher _pusher;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            INotificationPusher pusher,
            UserManager<User> userManager,
            AppDbContext context,
            ILogger<NotificationService> logger
        )
        {
            _repository = repository;
            _pusher = pusher;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

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
                    CreatedAt = DateTime.UtcNow,
                };

                await _repository.CreateAsync(notification, ct);

                // Get unread count for badge
                var unreadCount = await _repository.GetUnreadCountAsync(userId, ct);

                // Push real-time notification
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
                // Get all users with the specified role
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                if (!usersInRole.Any())
                {
                    _logger.LogWarning("No users found with role {RoleName}", roleName);
                    return;
                }

                var notifications = usersInRole
                    .Select(user => new Notification
                    {
                        UserId = user.Id,
                        Title = title,
                        Message = message,
                        NotificationType = notificationType,
                        ActionUrl = actionUrl,
                        ReferenceId = referenceId,
                        CreatedByUserId = createdByUserId,
                        CreatedAt = DateTime.UtcNow,
                    })
                    .ToList();

                await _repository.CreateManyAsync(notifications, ct);

                // Push real-time notifications to all users
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

        public async Task NotifyProcurementPublishedAsync(
            string procurementId,
            string procNum,
            string publishedByUserId,
            string publishedByUserName,
            CancellationToken ct = default
        )
        {
            var title = "Procurement Baru Dipublish";
            var message =
                $"Procurement {procNum} telah dipublish oleh {publishedByUserName}. Silakan review dan pickup.";
            var actionUrl = $"/ProcurementOrder";

            await SendNotificationToRoleAsync(
                "AP-PO",
                title,
                message,
                NotificationTypes.ProcurementPublished,
                actionUrl,
                procurementId,
                publishedByUserId,
                ct
            );
        }

        public async Task NotifyDocumentApprovedAsync(
            string prId,
            string prNumber,
            string approverRole,
            string approverUserId,
            string approverUserName,
            string nextApproverRole,
            CancellationToken ct = default
        )
        {
            try
            {
                // Get PR to find AP-PO user
                var pr = await _context
                    .PurchaseRequisitions.Include(p => p.Procurements)
                    .FirstOrDefaultAsync(p => p.PrId == prId, ct);

                if (pr == null)
                    return;

                var procurement = pr.Procurements?.FirstOrDefault();
                if (procurement == null)
                    return;

                string notificationType;
                string targetUserId;
                string title;
                string message;

                // Determine notification type based on approver role
                if (approverRole.Contains("Analyst", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByAnalyst;
                    // Notify next approver (Assistant Manager)
                    targetUserId = procurement.AssistantManagerUserId ?? "";
                    title = "Menunggu Approval Anda";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Analyst HTE & LTS). Silakan review dan approve.";
                }
                else if (approverRole.Contains("Assistant", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByAssistantManager;
                    // Notify next approver (Manager)
                    targetUserId = procurement.ManagerUserId ?? "";
                    title = "Menunggu Approval Anda";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Asst. Manager). Silakan review dan approve.";
                }
                else if (approverRole.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByManager;
                    // Notify AP-PO that all approvals complete
                    targetUserId = procurement.AppoUserId ?? "";
                    title = "Approval Selesai";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Manager). Semua approval selesai, silakan lanjutkan ke ISPA.";
                }
                else
                {
                    return;
                }

                if (string.IsNullOrEmpty(targetUserId))
                    return;

                var actionUrl = $"/PurchaseRequisitions/Details/{prId}";

                await SendNotificationAsync(
                    targetUserId,
                    title,
                    message,
                    notificationType,
                    actionUrl,
                    prId,
                    approverUserId,
                    ct
                );

                // Also notify AP-PO about approval progress
                if (
                    !string.IsNullOrEmpty(procurement.AppoUserId)
                    && procurement.AppoUserId != targetUserId
                )
                {
                    var appoTitle = "Update Status Approval";
                    var appoMessage =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} ({approverRole}).";

                    await SendNotificationAsync(
                        procurement.AppoUserId,
                        appoTitle,
                        appoMessage,
                        notificationType,
                        actionUrl,
                        prId,
                        approverUserId,
                        ct
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send document approved notification for PR {PrId}",
                    prId
                );
            }
        }

        public async Task NotifyPrCompletedAsync(
            string prId,
            string prNumber,
            string appoUserId,
            CancellationToken ct = default
        )
        {
            var title = "PR Selesai";
            var message = $"PR {prNumber} telah selesai dengan status Done PO.";
            var actionUrl = $"/PurchaseRequisitions/Details/{prId}";

            await SendNotificationAsync(
                appoUserId,
                title,
                message,
                NotificationTypes.PrCompleted,
                actionUrl,
                prId,
                null,
                ct
            );
        }

        public async Task NotifyPrRejectedAsync(
            string prId,
            string prNumber,
            string rejectedByUserId,
            string rejectedByUserName,
            string rejectionNote,
            string appoUserId,
            CancellationToken ct = default
        )
        {
            var title = "PR Ditolak";
            var message =
                $"PR {prNumber} ditolak oleh {rejectedByUserName}. Alasan: {rejectionNote}";
            var actionUrl = $"/PurchaseRequisitions/Details/{prId}";

            await SendNotificationAsync(
                appoUserId,
                title,
                message,
                NotificationTypes.PrRejected,
                actionUrl,
                prId,
                rejectedByUserId,
                ct
            );
        }

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
