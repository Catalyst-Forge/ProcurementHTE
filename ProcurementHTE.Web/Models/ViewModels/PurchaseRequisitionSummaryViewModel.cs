using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class PurchaseRequisitionSummaryViewModel
        {
            [DisplayName("PR ID")]
            public string PrId { get; set; } = string.Empty;
    
            [DisplayName("PR Number")]
            public string PrNumber { get; set; } = string.Empty;
    
            [DisplayName("Request Date")]
            public DateTime RequestDate { get; set; }
    
            [DisplayName("Description")]
            public string? Description { get; set; }
    
            [DisplayName("Created By")]
            public string CreatedBy { get; set; } = string.Empty;
    
            [DisplayName("Created At")]
            public DateTime CreatedAt { get; set; }
    
            [DisplayName("Procurement Count")]
            public int ProcurementCount { get; set; }
        }
}

