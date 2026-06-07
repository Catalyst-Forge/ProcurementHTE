using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApproval(string procurementId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "User tidak teridentifikasi.";
            return RedirectToAction(nameof(Details), new { id = procurementId });
        }

        var result = await _trackingService.SendForApprovalAsync(procurementId, userId, ct);
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Details), new { id = procurementId });
    }

    [HttpGet]
    public async Task<IActionResult> GenerateQrCode(string procurementId, CancellationToken ct)
    {
        var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);
        if (tracking == null)
            return NotFound();

        if (string.IsNullOrEmpty(tracking.ApprovalToken))
            return BadRequest("Approval token tidak tersedia.");

        var deepLink = $"procurehte://approve/{tracking.ApprovalToken}";
        var pngBytes = _qrCodeGenerator.GenerateAsPng(deepLink, 10);

        return File(pngBytes, "image/png", "approval-qr.png");
    }
}
