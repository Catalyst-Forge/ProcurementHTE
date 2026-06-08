using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Web.Mappers;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [HttpGet("Procurements/{procurementId}/CreateProfitLoss")]
    public async Task<IActionResult> CreateProfitLoss(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
        {
            TempData["ErrorMessage"] = "Procurement ID tidak valid";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
            {
                TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var existingPnl = await _pnlService.GetByProcurementAsync(procurementId);
            if (existingPnl != null)
            {
                TempData["WarningMessage"] =
                    "Procurement ini sudah memiliki Profit & Loss. Anda akan mengedit data yang sudah ada.";
                return RedirectToAction(nameof(EditProfitLoss), new { id = existingPnl.ProfitLossId });
            }

            var vendors = (await _vendorService.GetAllVendorsAsync())?.ToList() ?? [];
            if (vendors.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "Tidak ada vendor yang tersedia. Tambahkan vendor terlebih dahulu.";
                return RedirectToAction(nameof(Details), new { id = procurementId });
            }

            var viewModel = BuildProfitLossInputViewModel(procurement, vendors);
            await SetProfitLossViewBagsAsync(procurement);

            if (viewModel.OfferItems.Count == 0)
            {
                TempData["WarningMessage"] =
                    "Tidak ada item penawaran (ProcOffer). Tambahkan item penawaran di halaman Edit Procurement terlebih dahulu untuk dapat menginput harga per item.";
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProfitLossPost(
        [FromForm] ProfitLossInputViewModel viewModel
    )
    {
        RemoveProfitLossItemProcOfferValidation();

        if (!string.IsNullOrWhiteSpace(viewModel.ProcurementId))
            await RepopulateVendorChoices(viewModel);

        ValidateProfitLossFields(viewModel, requireProfitLossId: false);
        if (!ModelState.IsValid)
            return View("CreateProfitLoss", viewModel);

        var selectedVendors = GetDistinctSelectedVendorIds(viewModel);
        if (selectedVendors.Count == 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.SelectedVendorIds),
                "Pilih minimal 1 vendor untuk membuat Profit & Loss"
            );
            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        ValidateProfitLossVendorOffers(viewModel);
        if (!ModelState.IsValid)
        {
            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        try
        {
            var dto = ProfitLossViewModelMapper.ToInputDto(viewModel, selectedVendors);
            var pnl = await _pnlService.SaveInputAndCalculateAsync(dto);
            if (pnl == null)
            {
                ModelState.AddModelError("", "Gagal menyimpan Profit & Loss. Result is null.");
                await RepopulateVendorChoices(viewModel);
                return View("CreateProfitLoss", viewModel);
            }

            await UploadRoundLettersAsync(viewModel, pnl.ProfitLossId);
            await GenerateProfitLossDocumentsAsync(dto);

            TempData["SuccessMessage"] =
                "Profit & Loss created successfully and documents (including SPMP) were generated.";
            return RedirectToAction(nameof(Details), new { id = dto.ProcurementId });
        }
        catch (KeyNotFoundException ex)
        {
            ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError("", $"Gagal menyimpan data ke database: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", $"Validasi gagal: {ex.Message}");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Terjadi kesalahan: {ex.Message}");
        }

        await RepopulateVendorChoices(viewModel);
        return View("CreateProfitLoss", viewModel);
    }
}
