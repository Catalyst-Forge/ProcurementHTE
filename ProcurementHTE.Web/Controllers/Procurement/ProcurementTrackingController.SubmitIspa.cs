using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("SubmitIspa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitIspa(
        string procurementId,
        string ispaNumber,
        DateTime ispaDate,
        DateTime ispaSubmitDate,
        IFormFile ispaFile,
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

        var validationMessage = ValidateIspaFile(ispaFile);
        if (validationMessage != null)
        {
            TempData["Error"] = validationMessage;
            return RedirectToDetails(procurementId);
        }

        var result = await _trackingService.SubmitIspaAsync(
            procurementId,
            ispaNumber,
            userId,
            ispaDate,
            ispaSubmitDate,
            ispaFile.FileName,
            ispaFile.ContentType,
            ispaFile.Length,
            ispaFile.OpenReadStream(),
            ct
        );

        SetTrackingResultMessage(result.Success, result.Message);
        return RedirectToDetails(procurementId);
    }

    private static string? ValidateIspaFile(IFormFile? ispaFile)
    {
        if (ispaFile == null || ispaFile.Length == 0)
            return "File dokumen ISPA wajib diupload.";

        var ext = Path.GetExtension(ispaFile.FileName).ToLowerInvariant();
        var isPdf = ext == ".pdf" || ispaFile.ContentType.ToLower() == "application/pdf";
        if (!isPdf)
            return "Format file ISPA harus PDF.";

        const long maxFileSize = 50 * 1024 * 1024;
        return ispaFile.Length > maxFileSize ? "Ukuran file ISPA maksimal 50MB." : null;
    }
}
