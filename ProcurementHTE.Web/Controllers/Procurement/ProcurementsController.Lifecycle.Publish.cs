using System.Security.Claims;
using ProcurementHTE.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Authorization;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Roles = ActionRoles.Cancel)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "Procurement tidak valid";
            return RedirectToAction(nameof(Index));
        }

        var result = await _trackingService.CancelProcurementAsync(id, userId, ct);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ActionRoles.Procurement)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Procurement.Edit)]
    public async Task<IActionResult> Publish(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["ErrorMessage"] = "ID Procurement tidak valid";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var profitLoss = await _pnlQueryService.GetByProcurementAsync(id);
            if (profitLoss == null)
            {
                TempData["ErrorMessage"] =
                    "Tidak dapat publish procurement. Buat Profit & Loss terlebih dahulu.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var procurement = await _queryService.GetProcurementByIdAsync(id);
            await _workflowService.PublishAsync(id);
            await NotifyProcurementPublishedAsync(id, procurement);

            TempData["SuccessMessage"] =
                "Procurement berhasil dipublish dan langsung masuk tabel AP-PO.";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Terjadi kesalahan saat publish procurement: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = ActionRoles.Procurement)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Procurement.Edit)]
    public async Task<IActionResult> Unpublish(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["ErrorMessage"] = "ID Procurement tidak valid";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _workflowService.UnpublishAsync(id);
            TempData["SuccessMessage"] =
                "Publish procurement berhasil dibatalkan. Status kembali menjadi 'Created'.";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Terjadi kesalahan saat membatalkan publish procurement: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
