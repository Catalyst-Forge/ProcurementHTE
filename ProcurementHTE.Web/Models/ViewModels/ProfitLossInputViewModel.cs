using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProfitLossInputViewModel
    {
        [Required]
        public string WorkOrderId { get; set; } = null!;

        [DisplayName("Tarif 400 km")]
        [Range(0, double.MaxValue)]
        public decimal TarifAwal { get; set; }

        [DisplayName("Tarif Add")]
        [Range(0, double.MaxValue)]
        public decimal TarifAdd { get; set; }

        [DisplayName("KM per 25 km")]
        [Range(0, int.MaxValue)]
        public int KmPer25 { get; set; }

        public List<VendorOfferInputViewModel> Vendors { get; set; } = [];

        public IEnumerable<VendorChoiceViewModel> VendorChoices { get; set; } = [];

        public List<string> SelectedVendorIds { get; set; } = [];
    }
}
