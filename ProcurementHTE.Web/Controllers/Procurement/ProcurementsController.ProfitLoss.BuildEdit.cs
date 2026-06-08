using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task<ProfitLossEditViewModel> BuildProfitLossEditViewModelAsync(string profitLossId)
    {
        var dto = await _pnlService.GetEditDataAsync(profitLossId);
        var vendors = await _vendorService.GetAllVendorsAsync();
        var procurement =
            await _queryService.GetProcurementByIdAsync(dto.ProcurementId)
            ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

        var offerItems = BuildProfitLossOfferItems(procurement);
        var viewModel = new ProfitLossEditViewModel
        {
            ProfitLossId = dto.ProfitLossId,
            ProcurementId = dto.ProcurementId,
            JobTypeName = procurement.JobType?.TypeName,
            AccrualAmount = dto.AccrualAmount,
            RealizationAmount = dto.RealizationAmount,
            Distance = dto.Distance,
            TglMulaiSewa = dto.TglMulaiSewa,
            TglMulaiMoving = dto.TglMulaiMoving,
            Items = BuildProfitLossEditItems(dto.Items ?? [], offerItems),
            Vendors = BuildProfitLossVendorModels(dto.Vendors ?? [], procurement),
            SelectedVendorIds = ResolveSelectedVendorIds(dto),
            VendorChoices = BuildVendorChoices(vendors),
            OfferItems = offerItems,
        };

        await SetProfitLossViewBagsAsync(procurement);
        return viewModel;
    }

    private static List<string> ResolveSelectedVendorIds(ProfitLossEditDto dto)
    {
        var selectedVendorIds = (dto.SelectedVendorIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedVendorIds.Count > 0)
            return selectedVendorIds;

        return (dto.Vendors ?? [])
            .Select(v => v.VendorId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ItemTariffInputVm> BuildProfitLossEditItems(
        IEnumerable<ProfitLossItemInputDto> dtoItems,
        List<ProcOfferLiteVm> offerItems
    )
    {
        var existingItemsMap = dtoItems.ToDictionary(
            item => item.ProcOfferId,
            StringComparer.OrdinalIgnoreCase
        );

        return offerItems
            .Select(offer => BuildProfitLossEditItem(offer, existingItemsMap))
            .ToList();
    }

    private static ItemTariffInputVm BuildProfitLossEditItem(
        ProcOfferLiteVm offer,
        Dictionary<string, ProfitLossItemInputDto> existingItemsMap
    )
    {
        if (existingItemsMap.TryGetValue(offer.ProcOfferId, out var existingItem))
        {
            return new ItemTariffInputVm
            {
                ProcOfferId = existingItem.ProcOfferId,
                Quantity = existingItem.Quantity,
                QtyItems = existingItem.QtyItems,
                TarifAwal = existingItem.TarifAwal,
                TarifAdd = existingItem.TarifAdd,
                KmPer25 = existingItem.KmPer25,
                OperatorCost = existingItem.OperatorCost,
                UnitRevenue = offer.UnitRevenue,
            };
        }

        return new ItemTariffInputVm
        {
            ProcOfferId = offer.ProcOfferId,
            Quantity = (int)offer.Quantity,
            QtyItems = (int)offer.Quantity,
            TarifAwal = 0m,
            TarifAdd = 0m,
            KmPer25 = 0m,
            OperatorCost = 0m,
            UnitRevenue = offer.UnitRevenue,
        };
    }
}
