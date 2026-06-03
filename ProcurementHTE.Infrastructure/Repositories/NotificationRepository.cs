using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Notification notification, CancellationToken ct = default)
        {
            await _context.Notifications.AddAsync(notification, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task CreateManyAsync(
            IEnumerable<Notification> notifications,
            CancellationToken ct = default
        )
        {
            await _context.Notifications.AddRangeAsync(notifications, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Notification?> GetByIdAsync(
            string notificationId,
            CancellationToken ct = default
        )
        {
            return await _context
                .Notifications.Include(n => n.CreatedByUser)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId, ct);
        }

        public async Task<List<NotificationDto>> GetByUserIdAsync(
            string userId,
            int skip = 0,
            int take = 20,
            bool unreadOnly = false,
            CancellationToken ct = default
        )
        {
            var query = _context
                .Notifications.AsNoTracking()
                .Include(n => n.CreatedByUser)
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    ActionUrl = n.ActionUrl,
                    ReferenceId = n.ReferenceId,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    CreatedByUserName =
                        n.CreatedByUser != null
                            ? n.CreatedByUser.FullName ?? n.CreatedByUser.UserName
                            : null,
                })
                .ToListAsync(ct);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
        {
            return await _context.Notifications.CountAsync(
                n => n.UserId == userId && !n.IsRead,
                ct
            );
        }

        public async Task MarkAsReadAsync(string notificationId, CancellationToken ct = default)
        {
            var notification = await _context.Notifications.FindAsync([notificationId], ct);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
        {
            await _context
                .Notifications.Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(n => n.IsRead, true)
                            .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                    ct
                );
        }

        public async Task DeleteAsync(string notificationId, CancellationToken ct = default)
        {
            await _context
                .Notifications.Where(n => n.NotificationId == notificationId)
                .ExecuteDeleteAsync(ct);
        }

        public async Task DeleteOldNotificationsAsync(
            int daysOld = 30,
            CancellationToken ct = default
        )
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            await _context
                .Notifications.Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ExecuteDeleteAsync(ct);
        }

        public async Task<PurchaseRequisitionNotificationInfo?> GetPrForNotificationAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            var pr = await _context
                .PurchaseRequisitions.AsNoTracking()
                .Include(p => p.Procurements)
                .FirstOrDefaultAsync(p => p.PrId == prId, ct);

            if (pr == null)
                return null;

            var procurement = pr.Procurements?.FirstOrDefault();

            return new PurchaseRequisitionNotificationInfo
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                AppoUserId = procurement?.AppoUserId,
                AssistantManagerUserId = procurement?.AssistantManagerUserId,
                ManagerUserId = procurement?.ManagerUserId,
            };
        }
    }
}
