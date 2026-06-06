using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Mappers;

public static class ProfitLossViewModelMapper
{
    public static ProfitLossSummaryViewModel ToSummaryViewModel(ProfitLossSummaryDto dto)
    {
        return new ProfitLossSummaryViewModel
        {
            ProfitLossId = dto.ProfitLossId,
            ProcurementId = dto.ProcurementId,

            TotalOperatorCost = dto.TotalOperatorCost,
            TotalRevenue = dto.TotalRevenue,
            AccrualAmount = dto.AccrualAmount,
            RealizationAmount = dto.RealizationAmount,
            Distance = dto.Distance,

            SelectedVendorId = dto.SelectedVendorId,
            SelectedVendorName = dto.SelectedVendorName,
            SelectedFinalOffer = dto.SelectedFinalOffer,
            Profit = dto.Profit,
            ProfitPercent = dto.ProfitPercent,

            CreatedAt = dto.CreatedAt,

            Items = dto.Items,
            SelectedVendorNames = dto.SelectedVendorNames?.ToList() ?? [],

            Rows = dto
                .VendorComparisons.Select(v =>
                    (v.VendorName, v.FinalOffer, v.Profit, v.ProfitPercent, v.IsSelected)
                )
                .ToList(),
        };
    }

    public static ProfitLossInputDto ToInputDto(
        ProfitLossInputViewModel vm,
        List<string> selectedVendors
    )
    {
        var qtyMap = (vm.OfferItems ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o.ProcOfferId))
            .ToDictionary(o => o.ProcOfferId, o => o.Quantity, StringComparer.OrdinalIgnoreCase);

        var items = (vm.Items ?? [])
            .Select(x => new ProfitLossItemInputDto
            {
                ProcOfferId = x.ProcOfferId,
                Quantity =
                    x.Quantity > 0
                        ? x.Quantity
                        : (qtyMap.TryGetValue(x.ProcOfferId, out var q) ? (int)q : x.Quantity),
                QtyItems =
                    x.QtyItems > 0
                        ? x.QtyItems
                        : (qtyMap.TryGetValue(x.ProcOfferId, out var qi) ? (int)qi : 1),
                TarifAwal = x.TarifAwal ?? 0m,
                TarifAdd = x.TarifAdd ?? 0m,
                KmPer25 = x.KmPer25 ?? 0m,
                OperatorCost = x.OperatorCost ?? 0m,
                UnitRevenue = x.UnitRevenue,
            })
            .ToList();

        var allowedVendors = new HashSet<string>(selectedVendors, StringComparer.OrdinalIgnoreCase);

        return new ProfitLossInputDto
        {
            ProcurementId = vm.ProcurementId,
            AccrualAmount = vm.AccrualAmount,
            RealizationAmount = vm.RealizationAmount,
            Distance = vm.Distance,
            TglMulaiSewa = vm.TglMulaiSewa,
            TglMulaiMoving = vm.TglMulaiMoving,
            Items = items,
            SelectedVendorIds = selectedVendors,
            Vendors = BuildVendorOfferDtos(vm.Vendors, allowedVendors),
        };
    }

    public static List<VendorItemOffersDto> BuildVendorOfferDtos(
        IEnumerable<VendorItemOfferInputVm>? vendorInputs,
        HashSet<string>? allowedVendorIds
    )
    {
        var result = new List<VendorItemOffersDto>();
        if (vendorInputs == null)
            return result;

        foreach (var vendor in vendorInputs)
        {
            var vendorId = vendor?.VendorId;
            if (string.IsNullOrWhiteSpace(vendorId))
                continue;

            if (allowedVendorIds != null && !allowedVendorIds.Contains(vendorId))
                continue;

            var dtoItems = new List<VendorOfferPerItemDto>();

            foreach (var item in vendor!.Items ?? Enumerable.Empty<VendorOfferPerItemInputVm>())
            {
                if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                    continue;

                if (!item.IsIncluded)
                    continue;

                var dto = new VendorOfferPerItemDto
                {
                    VendorId = vendorId,
                    ProcOfferId = item.ProcOfferId!,
                    Round = item.Round,
                    Prices = [],
                    Quantity = item.Quantity,
                    Trip = item.Trip,
                };

                var prices = item.Prices ?? [];

                for (int idx = 0; idx < prices.Count; idx++)
                {
                    var price = prices[idx];
                    if (price <= 0m)
                        continue;
                    dto.Prices.Add(price);
                }

                if (dto.Prices.Count > 0)
                    dtoItems.Add(dto);
            }

            if (dtoItems.Count > 0)
            {
                result.Add(
                    new VendorItemOffersDto
                    {
                        VendorId = vendorId,
                        Items = dtoItems,
                        Letters = (vendor.Letters ?? [])
                            .Select(l => l?.Trim() ?? string.Empty)
                            .ToList(),
                        LetterDocIds = (vendor.LetterDocIds ?? []).Where(x => x != null).ToList()!,
                    }
                );
            }
        }

        return result;
    }
}
