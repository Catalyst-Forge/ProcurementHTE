using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Policy = Permissions.Procurement.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        try
        {
            var procurement = await _procurementService.GetProcurementByIdAsync(id);
            return procurement == null ? NotFound() : View(procurement);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to load procurement for deletion: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Procurement.Delete)]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["ErrorMessage"] = "ID Procurement tidak valid";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var procurement = await _procurementService.GetProcurementByIdAsync(id);
            if (procurement == null)
            {
                TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var prId = procurement.PrId;
            var deletedDocsCount = await _procDocService.DeleteAllByProcurementAsync(id, currentUserId);

            await _pnlService.DeleteByProcurementAsync(id, currentUserId);
            await _procurementService.DeleteProcurementAsync(procurement, currentUserId);

            if (!string.IsNullOrEmpty(prId))
                await _trackingService.RecalculatePrStatusAsync(prId);

            TempData["SuccessMessage"] = deletedDocsCount > 0
                ? $"Procurement beserta {deletedDocsCount} dokumen terkait berhasil dihapus!"
                : "Procurement berhasil dihapus!";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            TempData["ErrorMessage"] =
                $"Gagal menghapus procurement. Data ini mungkin masih digunakan di tempat lain: {ex.InnerException?.Message ?? ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = $"Operasi tidak valid: {ex.Message}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Terjadi kesalahan saat menghapus procurement: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
