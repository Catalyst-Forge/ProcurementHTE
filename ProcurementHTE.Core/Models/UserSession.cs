using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class UserSession
    {
        [Key]
        public string UserSessionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = default!;

        public User User { get; set; } = null!;

        [StringLength(128)]
        public string? Device { get; set; }

        [StringLength(128)]
        public string? Browser { get; set; }

        [StringLength(512)]
        public string? UserAgent { get; set; }

        [StringLength(64)]
        public string? IpAddress { get; set; }

        [StringLength(128)]
        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public bool IsCurrent { get; set; }
    }
}
