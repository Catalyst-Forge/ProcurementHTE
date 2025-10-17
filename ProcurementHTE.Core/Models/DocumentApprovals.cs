using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class DocumentApprovals
    {
        [Key]
        public string DocumentApprovalId { get; set; } = Guid.NewGuid().ToString();

        public int Level { get; set; }

        public int SequenceOrder { get; set; }

        // Foreign Keys
        public string RoleId { get; set; } = null!;
        public string WoTypeDocumentId { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = default!;

        [ForeignKey("WoTypeDocumentId")]
        public WoTypeDocuments WoTypeDocument { get; set; } = default!;
    }
}
