using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    public async Task<ActionResult> Create()
    {
        await PopulateViewBagForCreate();
        return View(new PurchaseRequisitionCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(PurchaseRequisitionCreateViewModel model)
    {
        if (model.ProcurementIds == null || model.ProcurementIds.Count == 0)
        {
            ModelState.AddModelError(
                "ProcurementIds",
                "At least one procurement must be selected."
            );
        }

        if (!await ValidateCreateInputAsync(model))
            return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var prId = Guid.NewGuid().ToString();
            var objectKey = BuildPrDocumentObjectKey(prId, model.DocumentFile!.FileName);

            await using var stream = model.DocumentFile.OpenReadStream();
            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                stream,
                model.DocumentFile.Length,
                model.DocumentFile.ContentType,
                HttpContext.RequestAborted
            );

            var purchaseRequisition = new PurchaseRequisition
            {
                PrId = prId,
                PrNumber = model.PRNumber,
                RequestDate = model.RequestDate,
                Description = model.Description,
                DocumentFileName = model.DocumentFile.FileName,
                DocumentFilePath = objectKey,
                DocumentContentType = model.DocumentFile.ContentType,
                DocumentFileSize = model.DocumentFile.Length,
                CreatedByUserId = userId,
            };

            await _purchaseRequisitionCommandService.CreateAsync(
                purchaseRequisition,
                model.ProcurementIds ?? []
            );

            TempData["SuccessMessage"] =
                $"Purchase Requisition {purchaseRequisition.PrNumber} created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            await PopulateViewBagForCreate();
            return View(model);
        }
    }

    private async Task<bool> ValidateCreateInputAsync(PurchaseRequisitionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBagForCreate();
            return false;
        }

        if (!ValidateUploadedPrDocument(model.DocumentFile))
        {
            await PopulateViewBagForCreate();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(model.PRNumber))
        {
            var exists = await _purchaseRequisitionQueryService.IsPrNumberExistsAsync(model.PRNumber);
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

        return true;
    }
}
