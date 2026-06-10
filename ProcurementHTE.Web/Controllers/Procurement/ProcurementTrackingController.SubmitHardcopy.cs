using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("SubmitJustification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitJustification(
        string procurementId,
        IFormFile justificationFile,
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

        var validationMessage = ValidateHardcopyFile(justificationFile);
        if (validationMessage != null)
        {
            TempData["Error"] = validationMessage;
            return RedirectToDetails(procurementId);
        }

        var result = await _trackingService.SubmitJustificationAsync(
            procurementId,
            userId,
            justificationFile.FileName,
            justificationFile.ContentType,
            justificationFile.Length,
            justificationFile.OpenReadStream(),
            ct
        );

        SetTrackingResultMessage(result.Success, result.Message);
        return RedirectToDetails(procurementId);
    }

    private static string? ValidateHardcopyFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "File bukti hardcopy tidak boleh kosong.";

        if (file.Length > 10 * 1024 * 1024)
            return "Ukuran file maksimal 10MB.";

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        return allowedTypes.Contains(file.ContentType.ToLower())
            ? null
            : "Format file harus berupa gambar (JPEG, PNG, GIF).";
    }
}
