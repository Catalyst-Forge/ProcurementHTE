using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.ViewModels;

public class SimpleProfitLossViewModel
{
    [Required]
    public string ProcurementId { get; set; } = null!;

    public string? ProcNum { get; set; }

    public string? ContractType { get; set; }

    [DisplayName("Vendor Pemenang")]
    [Required(ErrorMessage = "Pilih vendor pemenang")]
    public string SelectedVendorId { get; set; } = null!;

    [DisplayName("Revenue")]
    [Required(ErrorMessage = "Revenue wajib diisi")]
    [Range(1, 999_999_999_999_999, ErrorMessage = "Revenue wajib diisi")]
    public decimal? Revenue { get; set; }

    [DisplayName("Cost ke Vendor")]
    [Required(ErrorMessage = "Cost wajib diisi")]
    [Range(1, 999_999_999_999_999, ErrorMessage = "Cost wajib diisi")]
    public decimal? Cost { get; set; }

    public IReadOnlyList<VendorOption> Vendors { get; set; } = [];

    public sealed record VendorOption(string VendorId, string VendorName);
}
