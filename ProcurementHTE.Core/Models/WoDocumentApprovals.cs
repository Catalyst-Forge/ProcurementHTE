using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("WoDocumentApprovals")]
public class WoDocumentApprovals
{
    [Key]
    public string WoDocumentApprovalId { get; set; } = Guid.NewGuid().ToString();

    // FK
    [Required] public string WorkOrderId { get; set; } = default!;   // → WorkOrders
    [Required] public string WoDocumentId { get; set; } = default!;  // → WoDocuments
    [Required] public string RoleId { get; set; } = default!;        // → AspNetRoles
    public string? ApproverId { get; set; }                          // → AspNetUsers (nullable)

    // Tahapan berjenjang
    [Required] public int Level { get; set; } = 1;        // 1,2,3...
    [Required] public int SequenceOrder { get; set; } = 1;

    // Uploaded / Pending / Approved / Rejected
    [Required, MaxLength(16)] public string Status { get; set; } = "Pending";

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }

    // navigations
    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = default!;
    [ForeignKey(nameof(WoDocumentId))] public WoDocuments WoDocument { get; set; } = default!;
    [ForeignKey(nameof(RoleId))] public Role Role { get; set; } = default!;
    [ForeignKey(nameof(ApproverId))] public User? Approver { get; set; }
}
