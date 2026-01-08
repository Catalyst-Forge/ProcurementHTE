using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class Notification
    {
        [Key]
        [MaxLength(450)]
        public string NotificationId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = null!;

        [MaxLength(500)]
        public string? ActionUrl { get; set; }

        [MaxLength(450)]
        public string? ReferenceId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User? CreatedByUser { get; set; }
    }

    public static class NotificationTypes
    {
        public const string ProcurementPublished = "ProcurementPublished";

        public const string ApprovedByAnalyst = "ApprovedByAnalyst";

        public const string ApprovedByAssistantManager = "ApprovedByAssistantManager";

        public const string ApprovedByManager = "ApprovedByManager";

        public const string PrRejected = "PrRejected";

        public const string PrCompleted = "PrCompleted";
    }
}
