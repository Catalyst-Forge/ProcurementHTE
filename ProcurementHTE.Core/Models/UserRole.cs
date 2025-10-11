using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = default!;

        public string RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = default!;
    }
}
