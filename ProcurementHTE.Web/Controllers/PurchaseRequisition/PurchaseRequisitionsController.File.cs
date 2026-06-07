using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    [HttpGet]
    public async Task<ActionResult> DownloadDocument(string id)
    {
        var pr = await _purchaseRequisitionService.GetByIdAsync(id);
        if (pr == null || string.IsNullOrEmpty(pr.DocumentFilePath))
            return NotFound();

        try
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["response-content-disposition"] =
                    $"attachment; filename=\"{Uri.EscapeDataString(pr.DocumentFileName ?? "document")}\"",
                ["response-content-type"] = pr.DocumentContentType ?? "application/octet-stream",
            };

            var url = await _objectStorage.GetPresignedUrlHeaderAsync(
                _storageOptions.Bucket,
                pr.DocumentFilePath,
                TimeSpan.FromMinutes(30),
                headers,
                HttpContext.RequestAborted
            );

            var client = _httpClientFactory.CreateClient("MinioProxy");
            var response = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            var contentType = string.IsNullOrWhiteSpace(pr.DocumentContentType)
                ? "application/octet-stream"
                : pr.DocumentContentType;

            return File(stream, contentType, pr.DocumentFileName ?? "document", true);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to download document: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PreviewDocumentUrl(string id)
    {
        var pr = await _purchaseRequisitionService.GetByIdAsync(id);
        if (pr == null || string.IsNullOrEmpty(pr.DocumentFilePath))
            return Json(new { ok = false, error = "Document not found." });

        try
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(pr.DocumentFileName))
            {
                headers["response-content-disposition"] =
                    $"inline; filename=\"{Uri.EscapeDataString(pr.DocumentFileName)}\"";
            }

            if (!string.IsNullOrWhiteSpace(pr.DocumentContentType))
                headers["response-content-type"] = pr.DocumentContentType;

            var url = await _objectStorage.GetPresignedUrlHeaderAsync(
                _storageOptions.Bucket,
                pr.DocumentFilePath,
                TimeSpan.FromMinutes(15),
                headers,
                HttpContext.RequestAborted
            );

            return Json(new { ok = true, url });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = $"Failed to create preview link: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProcurementDocument(
        string prId,
        string procurementId,
        string documentTypeId,
        IFormFile documentFile
    )
    {
        try
        {
            if (documentFile == null || documentFile.Length == 0)
                return Json(new { ok = false, error = "Please select a file to upload." });

            if (documentFile.Length > MaxFileSize)
                return Json(new { ok = false, error = "File size exceeds 10MB limit." });

            var fileExtension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf")
                return Json(new { ok = false, error = "Only PDF files are allowed." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { ok = false, error = "User not authenticated." });

            await using var stream = documentFile.OpenReadStream();
            var result = await _procDocumentService.UploadAsync(
                new UploadProcDocumentRequest
                {
                    ProcurementId = procurementId,
                    DocumentTypeId = documentTypeId,
                    FileName = documentFile.FileName,
                    ContentType = documentFile.ContentType,
                    Content = stream,
                    Size = documentFile.Length,
                    Description = $"Uploaded via PR Service ({prId})",
                    UploadedByUserId = userId,
                    NowUtc = DateTime.UtcNow,
                }
            );

            return Json(
                new
                {
                    ok = true,
                    message = "Document uploaded successfully.",
                    document = new
                    {
                        id = result.ProcDocumentId,
                        name = result.FileName,
                        size = result.Size,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }
}
