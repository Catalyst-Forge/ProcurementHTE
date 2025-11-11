namespace ProcurementHTE.Core.Models.DTOs;

public sealed class RejectionInfoDto
{
    public string? WoDocumentApprovalId { get; set; }
    public string? RejectedByUserId { get; set; }
    public string? RejectedByUserName { get; set; }
    public string? RejectedByFullName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectNote { get; set; }
    public int Level { get; set; }
    public int SequenceOrder { get; set; }
}
