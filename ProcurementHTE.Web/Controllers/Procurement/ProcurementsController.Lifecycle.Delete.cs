using ProcurementHTE.Web.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Roles = ActionRoles.Procurement)]
    [Authorize(Policy = Permissions.Procurement.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(id);
            return procurement == null ? NotFound() : View(procurement);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to load procurement for deletion: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = ActionRoles.Procurement)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Procurement.Delete)]
    public async Task<IActionResult> DeleteConfirmed(string id, [FromServices] IProcurementDeletionService deletion)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["ErrorMessage"] = "ID Procurement tidak valid";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var deletedDocsCount = await deletion.DeleteAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (deletedDocsCount is null)
            {
                TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                return NotFound();
            }

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
