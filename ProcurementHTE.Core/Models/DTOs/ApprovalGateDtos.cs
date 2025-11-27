namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class GateInfoDto
    {
        public string? ProcurementId { get; set; }
        public string? ProcDocumentId { get; set; }
        public int? Level { get; set; }
        public int? SequenceOrder { get; set; }
        public string? DocStatus { get; set; } // Status dokumen (Uploaded/Pending/Approved/Rejected)
        public List<RoleInfoDto> RequiredRoles { get; set; } = new();
    }
}
