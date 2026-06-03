using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models;

public class UnitType
{
    [Key]
    public string UnitTypeId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(20)]
    [DisplayName("Kode Satuan")]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [DisplayName("Nama Satuan")]
    public string Name { get; set; } = null!;

    [DisplayName("Aktif")]
    public bool IsActive { get; set; } = true;

    [DisplayName("Urutan")]
    public int SortOrder { get; set; } = 0;

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public ICollection<ProfitLossItem> ProfitLossItems { get; set; } = [];
    public ICollection<VendorOffer> VendorOffers { get; set; } = [];
}
