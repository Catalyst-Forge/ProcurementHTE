using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("Reject")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
    public async Task<IActionResult> Reject(
        string procurementId,
        string rejectionNote,
        CancellationToken ct
    )
    {
        var userId = CurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "User tidak teridentifikasi.";
            return RedirectToDetails(procurementId);
        }

        var tracking = await LoadTrackingOrSetError(procurementId, ct);
        if (tracking == null)
            return RedirectToAction(nameof(Index));

        if (!CanUserApprove(tracking))
        {
            TempData["Error"] = "Anda tidak memiliki akses untuk reject procurement ini.";
            return RedirectToDetails(procurementId);
        }

        var result = await _trackingService.RejectProcurementAsync(
            procurementId,
            rejectionNote,
            userId,
            ct
        );
        SetTrackingResultMessage(result.Success, result.Message);
        return RedirectToDetails(procurementId);
    }
}
