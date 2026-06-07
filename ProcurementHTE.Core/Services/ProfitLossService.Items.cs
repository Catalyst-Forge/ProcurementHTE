using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        private (
            decimal operatorCostTotal,
            decimal revenueTotal,
            List<ProfitLossItem> item
        ) ComputeItems(List<ProfitLossItemInputDto> input, string jobTypeName, decimal? distance)
        {
            var items = new List<ProfitLossItem>();
            decimal opTotal = 0m;
            decimal revTotal = 0m;

            foreach (var dto in input)
            {
                var (item, operatorCost, revenue) = BuildCalculatedItem(
                    dto,
                    jobTypeName,
                    distance
                );
                items.Add(item);
                opTotal += operatorCost;
                revTotal += revenue;
            }

            return (opTotal, revTotal, items);
        }

        private (decimal operatorCostTotal, decimal revenueTotal) ApplyUpdatedItems(
            ProfitLoss pnl,
            ProfitLossUpdateDto dto,
            string jobTypeName
        )
        {
            var itemsByOffer = pnl.Items.ToDictionary(
                x => x.ProcOfferId,
                StringComparer.OrdinalIgnoreCase
            );
            decimal opTotal = 0m;
            decimal revTotal = 0m;

            foreach (var input in dto.Items)
            {
                if (!itemsByOffer.TryGetValue(input.ProcOfferId, out var entity))
                {
                    entity = new ProfitLossItem
                    {
                        ProfitLossId = pnl.ProfitLossId,
                        ProcOfferId = input.ProcOfferId,
                        ItemName = "",
                    };
                    pnl.Items.Add(entity);
                    itemsByOffer[input.ProcOfferId] = entity;
                }

                var (calculated, operatorCost, revenue) = BuildCalculatedItem(
                    input,
                    jobTypeName,
                    dto.Distance
                );
                entity.UnitQty = calculated.UnitQty;
                entity.BasePrice = calculated.BasePrice;
                entity.TarifAdd = calculated.TarifAdd;
                entity.KmPer25 = calculated.KmPer25;
                entity.OperatorCost = calculated.OperatorCost;
                entity.Quantity = calculated.Quantity;
                entity.Revenue = revenue;

                opTotal += operatorCost;
                revTotal += revenue;
            }

            return (opTotal, revTotal);
        }

        private (ProfitLossItem item, decimal operatorCost, decimal revenue) BuildCalculatedItem(
            ProfitLossItemInputDto input,
            string jobTypeName,
            decimal? distance
        )
        {
            var usesQuantityForCalc = !_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName);
            var unitQty = usesQuantityForCalc ? input.QtyItems : input.Quantity;
            var quantityDurasi = usesQuantityForCalc ? input.Quantity : (decimal?)null;

            var item = new ProfitLossItem
            {
                ProcOfferId = input.ProcOfferId,
                UnitQty = unitQty,
                BasePrice = input.TarifAwal,
                TarifAdd = input.TarifAdd,
                KmPer25 = input.KmPer25,
                Quantity = quantityDurasi,
                ItemName = "",
            };

            var revenue = _jobTypeCalc.CalculateItemRevenue(item, jobTypeName, distance);
            decimal operatorCost;

            if (_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName))
            {
                var kmPer25 = _jobTypeCalc.CalculateKmPer25(distance ?? 0);
                operatorCost = _jobTypeCalc.CalculateOperatorCost(input.TarifAdd, kmPer25);
                item.KmPer25 = kmPer25;
                item.OperatorCost = operatorCost;
                item.Quantity = null;
            }
            else
            {
                operatorCost = 0m;
                item.TarifAdd = null;
                item.KmPer25 = null;
                item.OperatorCost = null;
            }

            item.Revenue = revenue;
            return (item, operatorCost, revenue);
        }
    }
}
