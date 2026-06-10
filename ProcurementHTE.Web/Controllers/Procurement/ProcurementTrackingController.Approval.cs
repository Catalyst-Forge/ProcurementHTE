using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementTrackingController
{
    [HttpPost("SendApproval")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApproval(string procurementId, CancellationToken ct)
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

        var result = await _trackingService.SendForApprovalAsync(procurementId, userId, ct);
        SetTrackingResultMessage(result.Success, result.Message);
        return RedirectToDetails(procurementId);
    }

    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic")]
    public async Task<IActionResult> Approve(
        string procurementId,
        string? approvalNote,
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

        if (!CanUserApprove(tracking))
        {
            TempData["Error"] = "Anda tidak memiliki akses untuk approve procurement ini.";
            return RedirectToDetails(procurementId);
        }

        var result = await _trackingService.HandleApprovalStatusChangeAsync(
            procurementId,
            "approve",
            userId,
            approvalNote ?? "Approved via web",
            ct
        );

        TempData[result ? "Success" : "Error"] = result
            ? "Procurement berhasil di-approve."
            : "Gagal melakukan approval. Silakan coba lagi.";

        return RedirectToDetails(procurementId);
    }
}
