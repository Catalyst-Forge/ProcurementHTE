using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Authorization;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
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
            var profitLoss = await _pnlService.GetByProcurementAsync(id);
            if (profitLoss == null)
            {
                TempData["ErrorMessage"] =
                    "Tidak dapat publish procurement. Buat Profit & Loss terlebih dahulu.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var procurement = await _procurementService.GetProcurementByIdAsync(id);
            await _procurementService.PublishAsync(id);
            await NotifyProcurementPublishedAsync(id, procurement);

            TempData["SuccessMessage"] =
                "Procurement berhasil dipublish dan status berubah menjadi 'Waiting Pickup'.";
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
            await _procurementService.UnpublishAsync(id);
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
