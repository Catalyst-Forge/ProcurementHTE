using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 25L * 1024 * 1024)]
    [RequestSizeLimit(25L * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        string ProcurementId,
        string DocumentTypeId,
        IFormFile File,
        string? Description
    )
    {
        if (string.IsNullOrWhiteSpace(ProcurementId) || string.IsNullOrWhiteSpace(DocumentTypeId))
        {
            TempData["ErrorMessage"] = "Missing required parameters.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        if (File is null || File.Length == 0)
        {
            TempData["ErrorMessage"] = "No file selected.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        if ((Path.GetExtension(File.FileName) ?? string.Empty).ToLowerInvariant() != ".pdf")
        {
            TempData["ErrorMessage"] = "Only PDF files are allowed.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var docType = await _docTypeRepo.GetByIdAsync(DocumentTypeId);
            var uploadFileName = $"{SanitizeFileNameBase(docType?.Name) ?? DocumentTypeId}.pdf";
            await using var stream = File.OpenReadStream();

            var result = await _docSvc.UploadAsync(
                new UploadProcDocumentRequest
                {
                    ProcurementId = ProcurementId,
                    DocumentTypeId = DocumentTypeId,
                    Content = stream,
                    Size = File.Length,
                    FileName = uploadFileName,
                    ContentType = "application/pdf",
                    Description = Description,
                    UploadedByUserId = userId,
                    NowUtc = DateTime.UtcNow,
                },
                HttpContext.RequestAborted
            );

            var message = $"Successfully uploaded \"{uploadFileName}\".";
            if (!IsAjaxRequest())
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
            }

            var (uploaded, total) = await _trackingService.GetDocumentCountAsync(ProcurementId);
            return Json(
                new
                {
                    ok = true,
                    message,
                    procurementId = ProcurementId,
                    documentTypeId = DocumentTypeId,
                    uploadedDocs = uploaded,
                    totalDocs = total,
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
            if (IsAjaxRequest())
            {
                return BadRequest(
                    new { ok = false, error = $"Failed to upload document: {ex.Message}" }
                );
            }

            TempData["ErrorMessage"] = $"Failed to upload document: {ex.Message}";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }
    }

    private bool IsAjaxRequest()
    {
        if (Request is null)
            return false;

        if (
            Request.Headers.TryGetValue("X-Requested-With", out var requestedWith)
            && requestedWith == "XMLHttpRequest"
        )
            return true;

        return Request.Headers.TryGetValue("Accept", out var acceptHeader)
            && acceptHeader.Any(h =>
                h != null && h.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string SanitizeFileNameBase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "document";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();

        foreach (var ch in name.Trim())
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        var result = sb.ToString().Replace(' ', '_').Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "document" : result;
    }
}
