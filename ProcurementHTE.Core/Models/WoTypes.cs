using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class WoTypes
    {
        [Key]
        public string WoTypeId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<WorkOrder> WorkOrders { get; set; } = [];
        public ICollection<WoTypeDocuments> WoTypeDocuments { get; set; } = [];
    }
}
