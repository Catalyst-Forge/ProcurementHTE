using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        public async Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(
            string procurementId
        )
        {
            var pnl = await _pnlRepository.GetByProcurementAsync(procurementId);
            if (pnl == null)
                return null!;

            var allVendors = await _vendorRepository.GetAllAsync();
            var selectedRows = await _pnlRepository.GetSelectedVendorsAsync(procurementId);
            var offers = await _voRepository.GetByProcurementAsync(procurementId);
            var totalRevenue = ProfitLossCalculator.SafeSum(pnl.Items.Select(item => item.Revenue));
            var totalOperatorCost = ProfitLossCalculator.SafeSum(
                pnl.Items.Select(item => item.OperatorCost ?? 0)
            );

            var selectedVendorNames = selectedRows
                .Select(row =>
                    allVendors.FirstOrDefault(vendor => vendor.VendorId == row.VendorId)
                        ?.VendorName ?? row.VendorId
                )
                .ToList();

            var requiredItemIds = pnl
                .Items.Select(item => item.ProcOfferId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var vendorComparisons = offers
                .GroupBy(vendorOffer => vendorOffer.VendorId)
                .Select(group =>
                {
                    var perItemCosts = group
                        .GroupBy(vendorOffer => vendorOffer.ProcOfferId)
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var ordered = g.OrderBy(vo => vo.Round).ToList();
                                var last = ordered.Last();
                                var minPrice = ordered.Min(vo => vo.Price);
                                return ProfitLossCalculator.ComputeVendorItemCost(
                                    minPrice,
                                    last.QuantityItem,
                                    last.QuantityOfUnit
                                );
                            },
                            StringComparer.OrdinalIgnoreCase
                        );

                    var requiredTotal = ProfitLossCalculator.ComputeRequiredTotal(
                        perItemCosts,
                        requiredItemIds
                    );
                    var finalOffer =
                        requiredTotal != decimal.MaxValue
                            ? requiredTotal
                            : ProfitLossCalculator.SafeSum(perItemCosts.Values);

                    var vendorName =
                        allVendors.FirstOrDefault(vendor => vendor.VendorId == group.Key)
                            ?.VendorName ?? group.Key;
                    var profit = totalRevenue - finalOffer;
                    var profitPercent = totalRevenue > 0 ? (profit / totalRevenue) * 100m : 0m;

                    return new VendorComparisonDto
                    {
                        VendorName = vendorName,
                        FinalOffer = finalOffer,
                        Profit = profit,
                        ProfitPercent = profitPercent,
                        IsSelected = pnl.SelectedVendorId == group.Key,
                    };
                })
                .OrderBy(row => row.FinalOffer)
                .ToList();

            var itemBreakdown = pnl
                .Items.Select(item =>
                {
                    var procOffer = pnl
                        .Items.Select(i => i.ProcOffer)
                        .FirstOrDefault(i => i.ProcOfferId == item.ProcOfferId);
                    var itemName = procOffer?.ItemPenawaran ?? item.ProcOfferId;
                    var unitRevenue = procOffer?.UnitRevenue
                        ?? (string?)(item.UnitType?.Name ?? item.UnitTypeId);
                    var quantity = item.Quantity.HasValue
                        ? (int?)Convert.ToInt32(item.Quantity.Value)
                        : null;

                    return (
                        item.ProcOfferId,
                        itemName,
                        item.UnitQty,
                        item.BasePrice,
                        item.TarifAdd,
                        item.KmPer25,
                        item.OperatorCost,
                        item.Revenue,
                        quantity,
                        unitRevenue,
                        procOffer?.Unit
                    );
                })
                .ToList();

            return new ProfitLossSummaryDto
            {
                ProfitLossId = pnl.ProfitLossId,
                ProcurementId = pnl.ProcurementId,
                TotalOperatorCost = totalOperatorCost,
                TotalRevenue = totalRevenue,
                AccrualAmount = pnl.AccrualAmount ?? totalRevenue,
                RealizationAmount = pnl.RealizationAmount ?? totalOperatorCost,
                Distance = pnl.Distance ?? 0,
                SelectedVendorId = pnl.SelectedVendorId ?? "",
                SelectedVendorName =
                    vendorComparisons.FirstOrDefault(vendor => vendor.IsSelected)?.VendorName
                    ?? allVendors.FirstOrDefault(vendor => vendor.VendorId == pnl.SelectedVendorId)
                        ?.VendorName
                    ?? pnl.SelectedVendorId,
                SelectedFinalOffer = pnl.SelectedVendorFinalOffer,
                Profit = pnl.Profit,
                ProfitPercent = pnl.ProfitPercent,
                Items = itemBreakdown,
                SelectedVendorNames = selectedVendorNames,
                VendorComparisons = vendorComparisons,
                CreatedAt = pnl.CreatedAt,
            };
        }
    }
}
