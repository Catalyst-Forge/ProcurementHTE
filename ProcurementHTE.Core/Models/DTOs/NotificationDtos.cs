namespace ProcurementHTE.Core.Models.DTOs
{
    public class NotificationDto
    {
        public string NotificationId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string? ActionUrl { get; set; }
        public string? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUserName { get; set; }
        public string TimeAgo => GetTimeAgo(CreatedAt);

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;
            if (span.TotalMinutes < 1)
                return "Baru saja";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} menit lalu";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} jam lalu";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} hari lalu";
            return dateTime.ToString("dd MMM yyyy");
        }
    }

    public class CreateNotificationRequest
    {
        public string? UserId { get; set; }
        public string? TargetRole { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string? ActionUrl { get; set; }
        public string? ReferenceId { get; set; }
        public string? CreatedByUserId { get; set; }
    }

    public class PushNotificationDto
    {
        public string NotificationId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationListResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
    }
}
