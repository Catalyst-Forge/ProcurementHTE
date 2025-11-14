using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

[Table("ProcDocumentApprovals")]
public class ProcDocumentApprovals
{
    [Key]
    public string ProcDocumentApprovalId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string ProcurementId { get; set; } = default!;

    [Required]
    public string ProcDocumentId { get; set; } = default!;

    [Required]
    public string RoleId { get; set; } = default!;

    public string? ApproverId { get; set; }

    [Required]
    public int Level { get; set; } = 1;

    [Required]
    public int SequenceOrder { get; set; } = 1;

    [Required, MaxLength(16)]
    public string Status { get; set; } = "Pending";

    public DateTime? ApprovedAt { get; set; }

    public string? Note { get; set; }

    [ForeignKey(nameof(ProcurementId))]
    public Procurement Procurement { get; set; } = default!;

    [ForeignKey(nameof(ProcDocumentId))]
    public ProcDocuments ProcDocument { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = default!;

    [ForeignKey(nameof(ApproverId))]
    public User? Approver { get; set; }
}
