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

        var doc = await _docSvc.GetByIdAsync(id);
        if (doc is null || string.IsNullOrWhiteSpace(doc.ObjectKey))
            return Json(new { ok = false, error = "Dokumen tidak ditemukan." });

        // The presigned URL is signed for the internal MinIO endpoint (loopback in
        // production), so the browser cannot open it. Hand out a same-origin URL that
        // streams the file through the app instead.
        var url = Url.Action(nameof(Preview), new { id });
        return Json(new { ok = true, url });
    }

    [HttpGet("ProcurementDocuments/Preview/{id}")]
    public async Task<IActionResult> Preview(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        var doc = await _docSvc.GetByIdAsync(id);
        if (doc is null || string.IsNullOrWhiteSpace(doc.ObjectKey))
            return NotFound();

        try
        {
            var url = await _docSvc.GetPresignedPreviewUrlAsync(
                id,
                TimeSpan.FromMinutes(5),
                HttpContext.RequestAborted
            );

            var client = _http.CreateClient("MinioProxy");
            var resp = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );
            if (!resp.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, "Gagal memuat dokumen.");

            var stream = await resp.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            var contentType = string.IsNullOrWhiteSpace(doc.ContentType)
                ? "application/octet-stream"
                : doc.ContentType;

            var disposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(
                string.IsNullOrWhiteSpace(doc.FileName) ? "document" : doc.FileName
            );
            Response.Headers.ContentDisposition = disposition.ToString();
            Response.Headers.CacheControl = "private, no-store";

            return File(stream, contentType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, $"Gagal memuat dokumen: {ex.Message}");
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
