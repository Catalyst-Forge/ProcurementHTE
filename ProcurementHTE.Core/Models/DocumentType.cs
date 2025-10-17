using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class DocumentType
    {
        [Key]
        public string DocumentTypeId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public ICollection<WoDocuments> WoDocuments { get; set; } = [];
        public ICollection<WoTypeDocuments> WoTypeDocuments { get; set; } = [];
    }
}
