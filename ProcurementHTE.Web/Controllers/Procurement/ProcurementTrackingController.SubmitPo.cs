using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("SubmitPo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPo(
        string procurementId,
        string poNumber,
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

        if (!CanUserModifyProcurement(tracking))
        {
            TempData["Error"] = "Anda tidak memiliki akses untuk mengupdate procurement ini.";
            return RedirectToDetails(procurementId);
        }

        var result = await _trackingService.SubmitPoAsync(procurementId, poNumber, userId, ct);
        SetTrackingResultMessage(result.Success, result.Message);
        return RedirectToDetails(procurementId);
    }
}
