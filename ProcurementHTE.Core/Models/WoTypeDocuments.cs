using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoTypeDocuments
    {
        [Key]
        public int Id { get; set; }

        public bool IsMandatory { get; set; } = true;

        public int Sequence { get; set; }

        public bool IsGenerated { get; set; } = false;

        public bool IsUploadRequired { get; set; } = false;

        public bool RequiresApproval { get; set; } = false;

        public string? Note { get; set; }

        // Foreign Keys
        public int WoTypeId { get; set; }

        [ForeignKey("WoTypeId")]
        public WoTypes WoType { get; set; } = default!;

        public int DocumentTypeId { get; set; }

        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; } = default!;

        public ICollection<DocumentApprovals> DocumentApprovals { get; set; } = new List<DocumentApprovals>();
    }
}
