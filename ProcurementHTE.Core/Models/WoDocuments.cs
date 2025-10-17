using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDocuments
    {
        [Key]
        public string WoDocumentId { get; set; } = Guid.NewGuid().ToString();

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
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        public string WorkOrderId { get; set; } = null!;
        public string DocumentTypeId { get; set; } = null!;

        [ForeignKey("WorkOrderId")]
        public WorkOrder WorkOrder { get; set; } = default!;

        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; } = default!;

        public ICollection<WoDocumentApprovals> WoDocumentApprovals { get; set; } = [];
    }
}
