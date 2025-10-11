using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models
{
    public class WoDetail
    {
        [Key]
        public int WoDetailId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ItemName { get; set; } = null!;

        [Required]
        [DisplayName("Quantity")]
        public int Quantity { get; set; }

        [Required]
        [DisplayName("Unit")]
        public string Unit { get; set; } = null!;

        // Foreign Keys
        public string WoNum { get; set; } = null!;

        [ForeignKey("WoNum")]
        public virtual WorkOrder WorkOrder { get; set; } = null!;
    }
}
