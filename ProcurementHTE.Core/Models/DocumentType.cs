using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class DocumentType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public ICollection<WoDocuments> WoDocuments { get; set; } = new List<WoDocuments>();

        public ICollection<WoTypeDocuments> WoTypeDocuments { get; set; } = new List<WoTypeDocuments>();
    }
}
