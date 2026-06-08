using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class RegionDistributionViewModel
    {
        [DisplayName("Region")]
        public string RegionName { get; set; } = string.Empty;

        [DisplayName("Count")]
        public int Count { get; set; }

        [DisplayName("Total Value")]
        public decimal TotalValue { get; set; }
    }
}

