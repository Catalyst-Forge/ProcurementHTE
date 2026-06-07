using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Mappers;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [HttpGet]
    public async Task<IActionResult> EditProfitLoss(string id)
    {
        try
        {
            var viewModel = await BuildProfitLossEditViewModelAsync(id);
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("CreateProfitLoss", viewModel);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error: {ex.Message}");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfitLoss(ProfitLossEditViewModel viewModel)
    {
        ValidateProfitLossFields(viewModel, requireProfitLossId: true);
        ValidateSelectedVendorIds(viewModel, "Pilih minimal 1 vendor");

        if (!ModelState.IsValid)
        {
            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        var distinctSelectedVendors = GetDistinctSelectedVendorIds(viewModel);
        if (distinctSelectedVendors.Count == 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.SelectedVendorIds),
                "Tidak ada vendor valid yang dipilih"
            );
            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        try
        {
            var update = BuildProfitLossUpdateDto(viewModel, distinctSelectedVendors);
            var pnlUpdated = await _pnlService.EditProfitLossAsync(update);

            await UploadRoundLettersAsync(viewModel, pnlUpdated.ProfitLossId);
            await GenerateSpmpAfterProfitLossUpdateAsync(viewModel.ProcurementId);

            TempData["SuccessMessage"] =
                "Profit & Loss updated successfully and SPMP document was generated.";
            return RedirectToAction(nameof(Details), new { id = viewModel.ProcurementId });
        }
        catch (KeyNotFoundException ex)
        {
            ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            ModelState.AddModelError("", "Data telah diubah oleh pengguna lain. Silakan refresh halaman dan coba lagi.");
            TempData["ErrorMessage"] =
                $"Conflict: Data telah diubah ({ex.InnerException?.Message ?? ex.Message})";
        }
        catch (DbUpdateException ex)
        {
            ModelState.AddModelError("", $"Gagal mengupdate data ke database: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
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

    private static ProfitLossUpdateDto BuildProfitLossUpdateDto(
        ProfitLossEditViewModel viewModel,
        List<string> selectedVendorIds
    )
    {
        var update = new ProfitLossUpdateDto
        {
            ProfitLossId = viewModel.ProfitLossId,
            ProcurementId = viewModel.ProcurementId,
            AccrualAmount = viewModel.AccrualAmount,
            RealizationAmount = viewModel.RealizationAmount,
            Distance = viewModel.Distance,
            TglMulaiSewa = viewModel.TglMulaiSewa,
            TglMulaiMoving = viewModel.TglMulaiMoving,
            Items = (viewModel.Items ?? []).Select(ToProfitLossItemInputDto).ToList(),
            SelectedVendorIds = selectedVendorIds,
        };

        var allowedVendorSet = new HashSet<string>(selectedVendorIds, StringComparer.OrdinalIgnoreCase);
        update.Vendors = ProfitLossViewModelMapper.BuildVendorOfferDtos(
            viewModel.Vendors,
            allowedVendorSet
        );

        return update;
    }

    private static ProfitLossItemInputDto ToProfitLossItemInputDto(ItemTariffInputVm item)
    {
        return new ProfitLossItemInputDto
        {
            ProcOfferId = item.ProcOfferId,
            Quantity = item.Quantity,
            QtyItems = item.QtyItems,
            TarifAwal = item.TarifAwal ?? 0m,
            TarifAdd = item.TarifAdd ?? 0m,
            KmPer25 = item.KmPer25 ?? 0m,
            OperatorCost = item.OperatorCost ?? 0m,
            UnitRevenue = item.UnitRevenue,
        };
    }
}
