using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoTypeDocuments
    {
        [Key]
        public string WoTypeDocumentId { get; set; } = Guid.NewGuid().ToString();

        public bool IsMandatory { get; set; } = true;

        public int Sequence { get; set; }

        public bool IsGenerated { get; set; } = false;

        public bool IsUploadRequired { get; set; } = false;

        public bool RequiresApproval { get; set; } = false;

        public string? Note { get; set; }

        // Foreign Keys
        public string WoTypeId { get; set; } = null!;
        public string DocumentTypeId { get; set; } = null!;

        [ForeignKey("WoTypeId")]
        public WoTypes WoType { get; set; } = default!;

        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; } = default!;

        public ICollection<DocumentApprovals> DocumentApprovals { get; set; } = [];
    }
}
