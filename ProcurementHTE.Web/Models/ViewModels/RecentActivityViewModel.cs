using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class RecentActivityViewModel
        {
            [DisplayName("Time")]
            public DateTime Time { get; set; }
    
            [DisplayName("User")]
            public string User { get; set; } = string.Empty;
    
            [DisplayName("Action")]
            public string Action { get; set; } = string.Empty;
    
            [DisplayName("Description")]
            public string? Description { get; set; }
        }
}

