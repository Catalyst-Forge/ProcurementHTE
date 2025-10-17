using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDocumentApprovals
    {
        [Key]
        public string WoDocumentApprovalId { get; set; } = Guid.NewGuid().ToString();

        public string Status { get; set; } = null!;

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? ApprovedAt { get; set; }

        public string? Note { get; set; }

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string WoDocumentId { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        public string ApproverId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WoDocuments WorkOrder { get; set; } = default!;

        [ForeignKey("WoDocumentId")]
        public WoDocuments WoDocument { get; set; } = default!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = default!;

        [ForeignKey("ApproverId")]
        public User Approver { get; set; } = default!;
    }
}
