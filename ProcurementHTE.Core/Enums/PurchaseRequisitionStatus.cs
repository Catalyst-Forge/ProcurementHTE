using System.ComponentModel;

namespace ProcurementHTE.Core.Enums
{
    /// <summary>
    /// Status tracking untuk Purchase Requisition (PR)
    /// Flow: OnCreateDP3 → WaitingApproval (3 levels) → OnSubmitISPA → OnSubmitHardcopy → OnSubmitPO → DonePO
    /// Rejected: Status akhir jika approval ditolak
    /// </summary>
    public enum PurchaseRequisitionStatus
    {
        [Description("On Create DP3 (APPO)")]
        OnCreateDP3 = 1,

        [Description("Waiting Approval Analyst HTE")]
        WaitingApprovalAnalyst = 2,

        [Description("Waiting Approval Asst. Manager")]
        WaitingApprovalAsstManager = 3,

        [Description("Waiting Approval Manager")]
        WaitingApprovalManager = 4,

        [Description("On Submit ISPA")]
        OnSubmitISPA = 5,

        [Description("On Submit Hardcopy")]
        OnSubmitHardcopy = 6,

        [Description("On Submit PO")]
        OnSubmitPO = 7,

        [Description("Done PO")]
        DonePO = 8,

        [Description("Rejected")]
        Rejected = 99
    }
}
