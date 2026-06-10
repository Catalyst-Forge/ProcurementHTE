using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    private bool CanUserModifyProcurement(ProcurementTrackingDto tracking)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Admin"))
            return true;

        return !string.IsNullOrEmpty(tracking.AppoUserId) && tracking.AppoUserId == userId;
    }

    private bool CanUserApprove(ProcurementTrackingDto tracking)
    {
        var isAdmin = User.IsInRole("Admin");
        var isAnalyst = User.IsInRole("Analyst HTE & LTS");
        var isAsstManager = User.IsInRole("Assistant Manager HTE");
        var isManager = User.IsInRole("Manager Transport & Logistic");

        if (isAdmin && tracking.IsWaitingApproval)
            return true;

        return tracking.CurrentStatus switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => isAnalyst,
            ProcurementStatus.WaitingApprovalAsstManager => isAsstManager,
            ProcurementStatus.WaitingApprovalManager => isManager,
            _ => false,
        };
    }

    private bool HasApproverRole()
    {
        return User.IsInRole("Admin")
            || User.IsInRole("Analyst HTE & LTS")
            || User.IsInRole("Assistant Manager HTE")
            || User.IsInRole("Manager Transport & Logistic");
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
