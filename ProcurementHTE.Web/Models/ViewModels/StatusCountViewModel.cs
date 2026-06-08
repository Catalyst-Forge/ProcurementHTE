using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class StatusCountViewModel
        {
            [DisplayName("Status")]
            public string StatusName { get; set; } = string.Empty;
    
            [DisplayName("Count")]
            public int Count { get; set; }
        }
}

