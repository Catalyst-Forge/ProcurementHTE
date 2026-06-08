using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ProcurementSummaryViewModel
        {
            [DisplayName("Proc No.")]
            public string ProcNum { get; set; } = string.Empty;
    
            [DisplayName("Job Type")]
            public string JobTypeName { get; set; } = string.Empty;
    
            [DisplayName("Status")]
            public string StatusName { get; set; } = string.Empty;
    
            [DisplayName("Created By")]
            public string CreatedBy { get; set; } = string.Empty;
    
            [DisplayName("Created Date")]
            public DateTime CreatedDate { get; set; }
    
            [DisplayName("Total Amount")]
            public decimal TotalAmount { get; set; }
        }
}

