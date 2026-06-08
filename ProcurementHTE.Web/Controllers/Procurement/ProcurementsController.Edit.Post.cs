using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Procurement.Edit)]
    public async Task<IActionResult> Edit(
        string id,
        ProcurementEditViewModel editViewModel,
        string? submitAction = "Created"
    )
    {
        if (id != editViewModel.ProcurementId)
        {
            ModelState.AddModelError("", "ID Procurement tidak sesuai");
            return NotFound();
        }

        var procForAuthCheck = await _queryService.GetProcurementByIdAsync(id);
        if (procForAuthCheck == null)
            return NotFound();

        if (!CanUserEditProcurementByStatus(procForAuthCheck))
        {
            TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit procurement dengan status ini.";
            return RedirectToAction(nameof(Details), new { id });
        }

        RemoveDetailsValidation();
        RemoveOffersValidation();
        await ValidateEditViewModelAsync(editViewModel, submitAction);

        if (!ModelState.IsValid)
        {
            await RepopulateEditViewModel(editViewModel);
            return View(editViewModel);
        }

        try
        {
            var existingProcurement = await _queryService.GetProcurementByIdAsync(id);
            if (existingProcurement == null)
            {
                TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            await _commandService.EditProcurementAsync(
                BuildProcurementForUpdate(editViewModel, existingProcurement),
                id,
                editViewModel.Details ?? [],
                editViewModel.Offers ?? []
            );

            TempData["SuccessMessage"] = "Procurement updated successfully!";
            return RedirectToAction(nameof(Details), new { id = editViewModel.ProcurementId });
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
            return NotFound();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            ModelState.AddModelError("", "Data telah diubah oleh pengguna lain. Silakan refresh halaman dan coba lagi.");
            TempData["ErrorMessage"] =
                $"Conflict: Data telah diubah oleh pengguna lain ({ex.InnerException?.Message ?? ex.Message})";
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError("", $"Gagal mengupdate data ke database: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Terjadi kesalahan saat mengupdate data: {ex.Message}");
        }

        await RepopulateEditViewModel(editViewModel);
        return View(editViewModel);
    }
}
