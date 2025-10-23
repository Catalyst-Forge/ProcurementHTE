using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class VendorOfferInputViewModel
    {
        [Required]
        public string VendorId { get; set; } = null!;

        [MinLength(1)]
        public List<decimal> Prices { get; set; } = [];
    }
}
