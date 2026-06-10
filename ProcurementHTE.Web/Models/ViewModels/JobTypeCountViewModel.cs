using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class JobTypeCountViewModel
    {
        [DisplayName("Job Type")]
        public string JobTypeName { get; set; } = string.Empty;

        [DisplayName("Count")]
        public int Count { get; set; }

        [DisplayName("Total Value")]
        public decimal TotalValue { get; set; }
    }
}

