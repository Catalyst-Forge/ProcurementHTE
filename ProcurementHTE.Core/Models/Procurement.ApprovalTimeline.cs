using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models;

public partial class Procurement
{
    // ===== Approval Timeline Tracking Fields (for LDP reporting) =====

    // Manager Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Manager Approval Start")]
    public DateTime? ManagerApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Manager Approval End")]
    public DateTime? ManagerApprovalEndAt { get; set; }

    // VP Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("VP Approval Start")]
    public DateTime? VpApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("VP Approval End")]
    public DateTime? VpApprovalEndAt { get; set; }

    // Operation Director Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Op Director Approval Start")]
    public DateTime? OpDirApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Op Director Approval End")]
    public DateTime? OpDirApprovalEndAt { get; set; }

    // President Director Approval Timeline
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("President Director Approval Start")]
    public DateTime? PresDirApprovalStartAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("President Director Approval End")]
    public DateTime? PresDirApprovalEndAt { get; set; }

    // ===== End Approval Timeline Tracking Fields =====
}
