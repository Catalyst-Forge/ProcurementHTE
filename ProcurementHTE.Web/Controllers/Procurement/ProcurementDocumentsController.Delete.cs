using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] string id, [FromForm] string procurementId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ok = await _docSvc.DeleteAsync(id, currentUserId);
            TempData[ok ? "success" : "error"] = ok ? "Document deleted." : "Document not found.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to delete document: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { procurementId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(
        [FromForm] string id,
        [FromForm] string procurementId,
        [FromForm] string documentTypeId
    )
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { ok = false, error = "Invalid document ID" });

        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var ok = await _docSvc.DeleteAsync(id, currentUserId);
            if (!ok)
                return Json(new { ok = false, error = "Document not found." });

            var (uploaded, total) = await _trackingService.GetDocumentCountAsync(procurementId);
            return Json(
                new
                {
                    ok = true,
                    message = "Document deleted successfully.",
                    procurementId,
                    documentTypeId,
                    uploadedDocs = uploaded,
                    totalDocs = total,
                }
            );
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = $"Failed to delete document: {ex.Message}" });
        }
    }

    [HttpPost("SendApprovalPerDoc")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApprovalPerDoc(
        [FromForm] string procDocumentId,
        [FromForm] string procurementId
    )
    {
        if (string.IsNullOrWhiteSpace(procDocumentId))
        {
            TempData["ErrorMessage"] = "Invalid document.";
            return RedirectToAction(nameof(Index), new { procurementId });
        }

        try
        {
            var userId = User?.Identity?.Name ?? "-";
            await _docSvc.SendApprovalAsync(procDocumentId, userId, HttpContext.RequestAborted);
            TempData["SuccessMessage"] =
                "Dokumen dikirim untuk approval. Status menjadi Pending Approval.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to send approval request: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { procurementId });
    }
}
