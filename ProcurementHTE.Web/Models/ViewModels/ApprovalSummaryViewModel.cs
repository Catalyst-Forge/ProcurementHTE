using System.ComponentModel;
using System;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ApprovalSummaryViewModel
        {
            [DisplayName("Proc No.")]
            public string ProcNum { get; set; } = string.Empty;
    
            [DisplayName("Document")]
            public string DocumentName { get; set; } = string.Empty;
    
            [DisplayName("Approval Role")]
            public string ApprovalRole { get; set; } = string.Empty;
    
            [DisplayName("Created Date")]
            public DateTime CreatedDate { get; set; }
    
            [DisplayName("Days Waiting")]
            public int DaysWaiting => (DateTime.Now - CreatedDate).Days;
        }
}

