using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Roles = ActionRoles.Procurement)]
    [HttpGet("Procurements/{procurementId}/SimpleProfitLoss")]
    public async Task<IActionResult> SimpleProfitLoss(string procurementId)
    {
        var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
        if (procurement == null)
            return NotFound();

        if (!procurement.UsesSimpleProfitLoss)
            return RedirectToAction(nameof(CreateProfitLoss), new { procurementId });

        var pnl = await _pnlQueryService.GetByProcurementAsync(procurementId);
        return View(
            new SimpleProfitLossViewModel
            {
                ProcurementId = procurementId,
                ProcNum = procurement.DisplayId,
                ContractType = procurement.ContractType.ToString(),
                SelectedVendorId = pnl?.SelectedVendorId ?? string.Empty,
                Revenue = pnl?.AccrualAmount,
                Cost = pnl?.SelectedVendorFinalOffer,
                Vendors = await LoadVendorOptionsAsync(),
            }
        );
    }

    [Authorize(Roles = ActionRoles.Procurement)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimpleProfitLossPost(SimpleProfitLossViewModel viewModel)
    {
        var procurement = await _queryService.GetProcurementByIdAsync(viewModel.ProcurementId);
        if (procurement == null)
            return NotFound();

        var firstOffer = procurement.ProcOffers?.FirstOrDefault();
        if (firstOffer == null)
            ModelState.AddModelError("", "Tambahkan item penawaran di halaman Edit Procurement terlebih dahulu.");

        if (!ModelState.IsValid || firstOffer == null)
        {
            viewModel.ProcNum = procurement.DisplayId;
            viewModel.ContractType = procurement.ContractType.ToString();
            viewModel.Vendors = await LoadVendorOptionsAsync();
            return View(nameof(SimpleProfitLoss), viewModel);
        }

        await _pnlCommandService.SaveSimpleAsync(
            new SimpleProfitLossDto(
                viewModel.ProcurementId,
                firstOffer.ProcOfferId,
                firstOffer.Qty,
                viewModel.SelectedVendorId,
                viewModel.Revenue!.Value,
                viewModel.Cost!.Value
            )
        );

        TempData["SuccessMessage"] = "Profit & Loss tersimpan.";
        await WarnIfPrOverdrawnAsync(viewModel.ProcurementId);
        if (procurement.ContractType == Core.Enums.ContractType.LTC)
            await GenerateSpmpForSimpleProfitLossAsync(viewModel.ProcurementId);

        return RedirectToAction(nameof(Details), new { id = viewModel.ProcurementId });
    }

    private async Task GenerateSpmpForSimpleProfitLossAsync(string procurementId)
    {
        try
        {
            var procurement =
                await _queryService.GetProcurementByIdAsync(procurementId)
                ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

            await GenerateSpmpDocumentAsync(
                procurement,
                await GetProcDocumentTypesByNameAsync(),
                procurementId,
                "Surat Perintah Mulai Pekerjaan (SPMP) auto-generated"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SPMP generation failed for procurement {ProcurementId}", procurementId);
            TempData["WarningMessage"] = "Profit & Loss tersimpan, tetapi SPMP gagal digenerate.";
        }
    }

    private async Task<IReadOnlyList<SimpleProfitLossViewModel.VendorOption>> LoadVendorOptionsAsync()
    {
        var vendors = await _vendorQueryService.GetAllVendorsAsync();
        return (vendors ?? [])
            .OrderBy(v => v.VendorName)
            .Select(v => new SimpleProfitLossViewModel.VendorOption(v.VendorId, v.VendorName))
            .ToList();
    }
}
