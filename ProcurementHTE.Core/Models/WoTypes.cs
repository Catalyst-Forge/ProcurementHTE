using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models
{
    public class WoTypes
    {
        [Key]
        public int WoTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

        public ICollection<WoTypeDocuments> WoTypeDocuments { get; set; } = new List<WoTypeDocuments>();
    }
}
