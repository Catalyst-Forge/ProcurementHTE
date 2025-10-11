using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDocumentApprovals
    {
        [Key]
        public int Id { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? ApprovedAt { get; set; }

        public string? Note { get; set; }

        // Foreign Keys
        public string WoNum { get; set; } = null!;

        [ForeignKey("WoNum")]
        public WoDocuments WorkOrder { get; set; } = default!;

        public string WoDocumentId { get; set; } = null!;

        [ForeignKey("WoDocumentId")]
        public WoDocuments WoDocument { get; set; } = default!;

        public string RoleId { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = default!;

        public string ApproverId { get; set; } = null!;

        [ForeignKey("ApproverId")]
        public User Approver { get; set; } = default!;
    }
}
