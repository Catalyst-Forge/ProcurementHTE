using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class VendorPerformanceViewModel
    {
        [DisplayName("Vendor Code")]
        public string VendorCode { get; set; } = string.Empty;

        [DisplayName("Vendor Name")]
        public string VendorName { get; set; } = string.Empty;

        [DisplayName("Offer Count")]
        public int OfferCount { get; set; }

        [DisplayName("Selected Count")]
        public int SelectedCount { get; set; }

        [DisplayName("Win Rate")]
        public decimal WinRate =>
            OfferCount > 0 ? Math.Round((decimal)SelectedCount / OfferCount * 100, 2) : 0;
    }
}

