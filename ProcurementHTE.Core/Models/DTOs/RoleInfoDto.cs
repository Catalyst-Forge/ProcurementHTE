namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class RoleInfoDto
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? ProcDocumentApprovalId { get; set; }
        public int? Level { get; set; }
        public int? SequenceOrder { get; set; }

        // dipakai di timeline & helper
        public string? Status { get; set; } // "Pending","Approved","Rejected"
        public string? Note { get; set; }
        public string? ApproverId { get; set; } // atau ApproverUserId → sesuaikan konsisten
        public string? ApproverFullName { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
