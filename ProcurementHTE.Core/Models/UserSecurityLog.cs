using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Models
{
    public class UserSecurityLog
    {
        [Key]
        public string UserSecurityLogId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = default!;

        public User User { get; set; } = null!;

        public SecurityLogEventType EventType { get; set; }

        public bool IsSuccess { get; set; }

        [StringLength(512)]
        public string? Description { get; set; }

        [StringLength(64)]
        public string? IpAddress { get; set; }

        [StringLength(256)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
