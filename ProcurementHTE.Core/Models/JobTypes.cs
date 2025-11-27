using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models;

public class JobTypes
{
    [Key]
    public string JobTypeId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    [DisplayName("Jenis Pekerjaan")]
    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<Procurement> Procurements { get; set; } = [];
    public ICollection<JobTypeDocuments> JobTypeDocuments { get; set; } = [];
}
