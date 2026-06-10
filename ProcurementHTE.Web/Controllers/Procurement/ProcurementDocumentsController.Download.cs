using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
    [HttpGet("ProcurementDocuments/Download/{id}")]
    public async Task<IActionResult> Download(string id, string? procurementId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var doc = await _docSvc.GetByIdAsync(id);
            if (doc is null)
                return NotFound();

            var url = await _docSvc.GetPresignedDownloadUrlAsync(
                id,
                TimeSpan.FromMinutes(30),
                HttpContext.RequestAborted
            );

            var client = _http.CreateClient("MinioProxy");
            var resp = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            var contentType = string.IsNullOrWhiteSpace(doc.ContentType)
                ? "application/octet-stream"
                : doc.ContentType;

            return File(
                stream,
                contentType,
                fileDownloadName: doc.FileName,
                enableRangeProcessing: true
            );
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to download document: {ex.Message}";
            return RedirectToAction(nameof(Index), new { procurementId });
        }
    }

    [HttpGet("ProcurementDocuments/PreviewUrl/{id}")]
    public async Task<IActionResult> PreviewUrl(string id, string? procurementId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var url = await _docSvc.GetPresignedPreviewUrlAsync(
                id,
                TimeSpan.FromMinutes(15),
                HttpContext.RequestAborted
            );
            return Json(new { ok = true, url });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = $"Failed to create preview link: {ex.Message}" });
        }
    }

    [HttpGet("ProcurementDocuments/QrUrl/{id}")]
    public async Task<IActionResult> QrUrl(string id)
    {
        try
        {
            var url = await _docSvc.GetPresignedQrUrlAsync(
                id,
                TimeSpan.FromMinutes(15),
                HttpContext.RequestAborted
            );
            return Json(new { ok = url != null, url });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = $"Failed to create QR link: {ex.Message}" });
        }
    }

    [HttpGet("ProcurementDocuments/DownloadQr/{id}")]
    public IActionResult DownloadQr(string id)
    {
        return NotFound("QR code sekarang dikelola di level Purchase Requisition.");
    }
}
