using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("ReturnForRevision")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
    public async Task<IActionResult> ReturnForRevision(
        string procurementId,
        int[] symptoms,
        string rejectionNote,
        CancellationToken ct
    )
    {
        var userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return AjaxOrRedirect(false, "User tidak teridentifikasi.", procurementId);

        var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
        if (tracking == null)
            return AjaxOrRedirect(false, "Procurement tidak ditemukan.", procurementId, redirectToIndex: true);

        if (!CanUserApprove(tracking))
            return AjaxOrRedirect(false, "Anda tidak memiliki akses untuk reject procurement ini.", procurementId);

        var symptomFlags = BuildSymptomFlags(symptoms);
        if (symptomFlags == RejectionSymptom.None)
            return AjaxOrRedirect(false, "Minimal satu symptom harus dipilih.", procurementId);

        var result = await _trackingService.ReturnForRevisionAsync(
            procurementId,
            symptomFlags,
            rejectionNote,
            userId,
            ct
        );

        return AjaxOrRedirect(result.Success, result.Message, procurementId);
    }

    [HttpPost("ResubmitRevision")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Operation, Analyst HTE & LTS, AP-PO")]
    public async Task<IActionResult> ResubmitRevision(
        string procurementId,
        CancellationToken ct
    )
    {
        var userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return AjaxOrRedirect(false, "User tidak teridentifikasi.", procurementId);

        var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
        if (tracking == null)
            return AjaxOrRedirect(false, "Procurement tidak ditemukan.", procurementId, redirectToIndex: true);

        if (!CanUserResubmitRevision(tracking))
            return AjaxOrRedirect(false, "Anda tidak memiliki akses untuk resubmit revision ini.", procurementId);

        var result = await _trackingService.ResubmitRevisionAsync(procurementId, userId, ct);
        return AjaxOrRedirect(result.Success, result.Message, procurementId);
    }

    private static RejectionSymptom BuildSymptomFlags(IEnumerable<int> symptoms)
    {
        var symptomFlags = RejectionSymptom.None;
        foreach (var symptom in symptoms)
            symptomFlags |= (RejectionSymptom)symptom;

        return symptomFlags;
    }
}
