using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    public async Task<ActionResult> Edit(string id)
    {
        var pr = await _purchaseRequisitionQueryService.GetByIdWithProcurementsAsync(id);
        if (pr == null)
            return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && pr.CreatedByUserId != currentUserId)
        {
            TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit PR ini.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateViewBagForCreate();
        return View(
            new PurchaseRequisitionEditViewModel
            {
                PrId = pr.PrId,
                PRNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                ExistingDocumentFileName = pr.DocumentFileName,
                ProcurementIds = pr.Procurements?.Select(p => p.ProcurementId).ToList() ?? [],
            }
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string id, PurchaseRequisitionEditViewModel model)
    {
        if (id != model.PrId)
            return BadRequest();

        if (!await ValidateEditInputAsync(model))
            return View(model);

        try
        {
            var existingPr = await _purchaseRequisitionQueryService.GetByIdAsync(id);
            if (existingPr == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && existingPr.CreatedByUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit PR ini.";
                return RedirectToAction(nameof(Index));
            }

            existingPr.PrNumber = model.PRNumber;
            existingPr.RequestDate = model.RequestDate;
            existingPr.Description = model.Description;

            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingPr.DocumentFilePath))
                    await SafeDeleteFromStorageAsync(existingPr.DocumentFilePath);

                var objectKey = BuildPrDocumentObjectKey(existingPr.PrId, model.DocumentFile.FileName);
                await using var stream = model.DocumentFile.OpenReadStream();
                await _objectStorage.UploadAsync(
                    _storageOptions.Bucket,
                    objectKey,
                    stream,
                    model.DocumentFile.Length,
                    model.DocumentFile.ContentType,
                    HttpContext.RequestAborted
                );

                existingPr.DocumentFileName = model.DocumentFile.FileName;
                existingPr.DocumentFilePath = objectKey;
                existingPr.DocumentContentType = model.DocumentFile.ContentType;
                existingPr.DocumentFileSize = model.DocumentFile.Length;
            }

            await _purchaseRequisitionCommandService.UpdateAsync(existingPr, model.ProcurementIds);
            TempData["SuccessMessage"] =
                $"Purchase Requisition {existingPr.PrNumber} updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            await PopulateViewBagForCreate();
            return View(model);
        }
    }

    private async Task<bool> ValidateEditInputAsync(PurchaseRequisitionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBagForCreate();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(model.PRNumber))
        {
            var exists = await _purchaseRequisitionQueryService.IsPrNumberExistsAsync(
                model.PRNumber,
                model.PrId
            );
            if (exists)
            {
                ModelState.AddModelError(
                    "PRNumber",
                    $"PR Number '{model.PRNumber}' sudah digunakan. Gunakan nomor lain."
                );
                await PopulateViewBagForCreate();
                return false;
            }
        }

        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            return ValidateUploadedPrDocument(model.DocumentFile, populateViewBag: true);
        }

        return true;
    }
}
