using System.ComponentModel;

namespace ProcurementHTE.Core.Enums
{
    /// <summary>
    /// Status tracking untuk Procurement (per-item level)
    /// Flow: OnCreateDP3 → WaitingApproval (3-6 levels based on CT) → OnSubmitISPA → OnSubmitHardcopy → OnSubmitPO → DonePO
    /// Approval levels based on CT (Contract Total / Final Offer PNL):
    /// - CT ≤ 500M: Analyst → Asst Manager → Manager
    /// - CT ≤ 5B: + VP
    /// - CT ≤ 10B: + VP → OpDir
    /// - CT > 10B: + VP → OpDir → PresDir
    /// Rejected: Status akhir jika approval ditolak
    /// </summary>
    public enum ProcurementStatus
    {
        [Description("On Create DP3 (APPO)")]
        OnCreateDP3 = 1,

        [Description("Waiting Approval Analyst HTE")]
        WaitingApprovalAnalyst = 2,

        [Description("Waiting Approval Asst. Manager")]
        WaitingApprovalAsstManager = 3,

        [Description("Waiting Approval Manager")]
        WaitingApprovalManager = 4,

        [Description("Waiting Approval Vice President")]
        WaitingApprovalVP = 5,

        [Description("Waiting Approval Operation Director")]
        WaitingApprovalOpDir = 6,

        [Description("Waiting Approval President Director")]
        WaitingApprovalPresDir = 7,

        [Description("On Submit ISPA")]
        OnSubmitISPA = 8,

        [Description("On Submit Hardcopy")]
        OnSubmitHardcopy = 9,

        [Description("On Submit PO")]
        OnSubmitPO = 10,

        [Description("Done PO")]
        DonePO = 11,

        /// <summary>
        /// Needs revision by PIC Ops/Creator - data issues
        /// </summary>
        [Description("Needs Revision (Data)")]
        NeedsRevisionData = 20,

        /// <summary>
        /// Needs revision by APPO - PR/document issues
        /// </summary>
        [Description("Needs Revision (PR/Dokumen)")]
        NeedsRevisionPR = 21,

        [Description("Rejected")]
        Rejected = 99
    }
}
