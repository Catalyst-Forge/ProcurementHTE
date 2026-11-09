using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    private bool CanUserModifyProcurement(ProcurementTrackingDto tracking)
    {
        // Nobody picks a job up any more, so every AP-PO user may act on it.
        return User.IsInRole("Admin") || User.IsInRole("AP-PO");
    }

    private bool CanUserApprove(ProcurementTrackingDto tracking)
    {
        var isAdmin = User.IsInRole("Admin");
        var isAnalyst = User.IsInRole("Analyst HTE & LTS");
        var isAsstManager = User.IsInRole(RoleNames.AsstManager);
        var isManager = User.IsInRole("Manager Transport & Logistic");
        var isVicePresident = User.IsInRole("Vice President");

        if (isAdmin && tracking.IsWaitingApproval)
            return true;

        return tracking.CurrentStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => isAnalyst,
            ProcurementStatus.WaitingApprovalAsstManager => isAsstManager,
            ProcurementStatus.WaitingApprovalManager => isManager,
            ProcurementStatus.WaitingApprovalVP => isVicePresident,
            _ => false,
        };
    }

    private bool HasApproverRole()
    {
        return User.IsInRole("Admin")
            || User.IsInRole("Analyst HTE & LTS")
            || User.IsInRole(RoleNames.AsstManager)
            || User.IsInRole("Manager Transport & Logistic")
            || User.IsInRole("Vice President");
    }

    private bool CanUserResubmitRevision(ProcurementTrackingDto tracking)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Admin"))
            return true;

        return tracking.CurrentStatus switch
        {
            ProcurementStatus.NeedsRevisionData =>
                User.IsInRole("Operation")
                || User.IsInRole("Analyst HTE & LTS")
                || (!string.IsNullOrEmpty(tracking.PicOpsUserId) && tracking.PicOpsUserId == userId),
            ProcurementStatus.NeedsRevisionPR =>
                User.IsInRole("AP-PO")
                || (!string.IsNullOrEmpty(tracking.AppoUserId) && tracking.AppoUserId == userId),
            _ => false,
        };
    }
}
