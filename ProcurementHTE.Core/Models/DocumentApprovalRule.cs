using ProcurementHTE.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    /// <summary>
    /// Approval rule per dokumen berdasarkan rentang nilai (CT PNL).
    /// </summary>
    [Table("DocumentApprovalRules")]
    public class DocumentApprovalRule
    {
        [Key]
        public string DocumentApprovalRuleId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string DocumentTypeId { get; set; } = null!;

        public string? JobTypeId { get; set; }

        public ProcurementCategory? ProcurementCategory { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxAmount { get; set; }

        [Required]
        public string SubmitterRoleId { get; set; } = null!;

        /// <summary>
        /// Optional: User ID spesifik untuk submitter. Jika diisi, akan digunakan untuk nama di dokumen.
        /// </summary>
        public string? SubmitterUserId { get; set; }

        public string? ApproverRoleId { get; set; }

        /// <summary>
        /// Optional: User ID spesifik untuk approver. Jika diisi, akan digunakan untuk nama di dokumen.
        /// </summary>
        public string? ApproverUserId { get; set; }

        public int Sequence { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public DocumentType? DocumentType { get; set; }
        public JobTypes? JobType { get; set; }
        public User? SubmitterUser { get; set; }
        public User? ApproverUser { get; set; }
    }
}
