using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDocuments
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string FileName { get; set; } = null!;

        [Required]
        public string FilePath { get; set; } = null!;

        [Required]
        public string Status { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public string WoNum { get; set; } = null!;

        [ForeignKey("WoNum")]
        public WorkOrder WorkOrder { get; set; } = default!;

        public int DocumentTypeId { get; set; }

        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; } = default!;

        public ICollection<WoDocumentApprovals> WoDocumentApprovals { get; set; } = new List<WoDocumentApprovals>();
    }
}
