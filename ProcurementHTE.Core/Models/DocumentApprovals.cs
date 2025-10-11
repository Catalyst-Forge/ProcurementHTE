using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class DocumentApprovals
    {
        [Key]
        public int Id { get; set; }

        public int Level { get; set; }

        public int SequenceOrder { get; set; }

        // Foreign Keys
        public string RoleId { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = default!;

        public int WoTypeDocumentId { get; set; }

        [ForeignKey("WoTypeDocumentId")]
        public WoTypeDocuments WoTypeDocument { get; set; } = default!;
    }
}
