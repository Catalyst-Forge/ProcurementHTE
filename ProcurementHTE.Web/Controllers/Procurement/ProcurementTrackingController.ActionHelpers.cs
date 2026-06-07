using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private IActionResult RedirectToDetails(string procurementId) =>
        RedirectToAction("Details", new { procurementId });

    private async Task<ProcurementTrackingDto?> LoadTrackingOrSetError(
        string procurementId,
        CancellationToken ct
    )
    {
        var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
        if (tracking == null)
            TempData["Error"] = "Procurement tidak ditemukan.";

        return tracking;
    }

    private void SetTrackingResultMessage(bool success, string? message)
    {
        if (success)
            TempData["Success"] = message ?? string.Empty;
        else
            TempData["Error"] = message ?? string.Empty;
    }

    private bool IsAjaxRequest() => Request.Headers.XRequestedWith == "XMLHttpRequest";

    private IActionResult AjaxOrRedirect(
        bool success,
        string? message,
        string procurementId,
        bool redirectToIndex = false
    )
    {
        if (IsAjaxRequest())
            return Json(new { success, message });

        TempData[success ? "Success" : "Error"] = message ?? string.Empty;
        return redirectToIndex ? RedirectToAction(nameof(Index)) : RedirectToDetails(procurementId);
    }
}
