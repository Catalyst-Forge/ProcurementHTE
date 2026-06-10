using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpGet("GenerateQrCode/{procurementId}")]
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

    [HttpGet("HardcopyEvidence/{procurementId}")]
    public async Task<IActionResult> GetHardcopyEvidence(
        string procurementId,
        CancellationToken ct
    )
    {
        var url = await _trackingService.GetHardcopyEvidenceUrlAsync(procurementId, ct);
        if (string.IsNullOrEmpty(url))
            return NotFound("Hardcopy evidence tidak ditemukan.");

        return Json(new { url });
    }

    [HttpGet("DownloadIspaFile/{procurementId}")]
    public async Task<IActionResult> DownloadIspaFile(string procurementId, CancellationToken ct)
    {
        var url = await _trackingService.GetIspaFileUrlAsync(procurementId, ct);
        if (string.IsNullOrEmpty(url))
        {
            TempData["Error"] = "File ISPA tidak ditemukan.";
            return RedirectToAction("Details", new { procurementId });
        }

        return Redirect(url);
    }
}
