// Core/DTOs/ApprovalTimelineDto.cs
namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApprovalTimelineDto
    {
        public string ProcurementId { get; set; } = default!;
        public string ProcDocumentId { get; set; } = default!;
        public string? DocStatus { get; set; }
        public int? CurrentGateLevel { get; set; }      // ← int?
        public int? CurrentGateSequence { get; set; }   // ← int?
        public List<RoleInfoDto> RequiredRoles { get; set; } = new();
        public List<ApprovalStepDto> Steps { get; set; } = new();
    }


    public sealed class ApprovalStepDto
    {
        public string? ProcDocumentApprovalId { get; set; }
        public int? Level { get; set; }          // ← int?
        public int? SequenceOrder { get; set; }  // ← int?
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? Status { get; set; }
        public string? AssignedApproverUserId { get; set; }
        public string? AssignedApproverFullName { get; set; }
        public string? ApproverUserId { get; set; }
        public string? ApproverFullName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Note { get; set; }
    }

}
