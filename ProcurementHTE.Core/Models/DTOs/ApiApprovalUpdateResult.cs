namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApprovalUpdateResult
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }     // NotYourTurn | Blocked | InvalidGateConfig | NoEligibleApprover | AlreadyFinalized | QrNotFound | ApprovalNotFound | InvalidAction | Error
        public string? Message { get; set; }
        public string? Action { get; set; }     // "approve" | "reject"
        public string? ApprovalId { get; set; }
        public string? ProcurementId { get; set; }
        public string? ProcDocumentId { get; set; }
        public string? DocStatus { get; set; }
        public int? CurrentGateLevel { get; set; }
        public int? CurrentGateSequence { get; set; }
        public List<RoleInfoDto> RequiredRoles { get; set; } = new();

        // Debug/UX helper
        public List<string> YourRoles { get; set; } = new();
        public bool? AlreadyApprovedByYou { get; set; }
        public int? YourLastApprovalLevel { get; set; }
        public int? YourLastApprovalSequence { get; set; }
        public DateTime? YourLastApprovalAt { get; set; }

        // detail untuk kasus Rejected
        public string? RejectedByUserId { get; set; }
        public string? RejectedByUserName { get; set; }
        public string? RejectedByFullName { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectNote { get; set; }


        public DateTime? When { get; set; }
    }
}
