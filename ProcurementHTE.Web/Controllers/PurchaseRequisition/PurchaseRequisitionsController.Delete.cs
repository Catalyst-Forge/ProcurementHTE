using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    public async Task<ActionResult> Delete(string id)
    {
        var pr = await _purchaseRequisitionService.GetByIdWithProcurementsAsync(id);
        if (pr == null)
            return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && pr.CreatedByUserId != currentUserId)
        {
            TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk menghapus PR ini.";
            return RedirectToAction(nameof(Index));
        }

        return View(pr);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirmed(string id)
    {
        try
        {
            var pr = await _purchaseRequisitionService.GetByIdAsync(id);
            if (pr == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && pr.CreatedByUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk menghapus PR ini.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(pr.DocumentFilePath))
                await SafeDeleteFromStorageAsync(pr.DocumentFilePath);

            await _purchaseRequisitionService.DeleteAsync(id, currentUserId);

            TempData["SuccessMessage"] =
                $"Purchase Requisition {pr.PrNumber} deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
