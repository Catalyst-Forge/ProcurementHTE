using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class DocumentType
    {
        [Key]
        public string DocumentTypeId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public ICollection<ProcDocuments> ProcDocuments { get; set; } = [];
        public ICollection<JobTypeDocuments> JobTypeDocuments { get; set; } = [];
    }
}
