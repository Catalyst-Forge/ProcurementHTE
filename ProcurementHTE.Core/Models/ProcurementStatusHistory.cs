using ProcurementHTE.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    /// <summary>
    /// History/Timeline untuk tracking perubahan status Procurement (per-item level)
    /// </summary>
    [Table("ProcurementStatusHistories")]
    public class ProcurementStatusHistory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ProcurementId { get; set; } = null!;

        [Required]
        public ProcurementStatus Status { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? ChangedByUserId { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ProcurementId))]
        public Procurement Procurement { get; set; } = null!;

        [ForeignKey(nameof(ChangedByUserId))]
        public User? ChangedByUser { get; set; }
    }
}
