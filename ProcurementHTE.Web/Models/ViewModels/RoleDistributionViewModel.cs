using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class RoleDistributionViewModel
        {
            [DisplayName("Role Name")]
            public string RoleName { get; set; } = string.Empty;
    
            [DisplayName("User Count")]
            public int UserCount { get; set; }
    
            [DisplayName("Color")]
            public string Color { get; set; } = "#6c757d";
        }
}

