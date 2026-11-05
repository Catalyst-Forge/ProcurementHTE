using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        private static (decimal revenueTotal, List<ProfitLossItem> items) BuildItems(
            List<ProfitLossItemInputDto> input,
            bool usesDuration
        )
        {
            var items = input.Select(dto => BuildItem(dto, usesDuration)).ToList();
            return (items.Sum(item => item.Revenue), items);
        }

        private static decimal ApplyUpdatedItems(
            ProfitLoss pnl,
            ProfitLossUpdateDto dto,
            bool usesDuration
        )
        {
            var itemsByOffer = pnl.Items.ToDictionary(
                x => x.ProcOfferId,
                StringComparer.OrdinalIgnoreCase
            );
            decimal revenueTotal = 0m;

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

                var updated = BuildItem(input, usesDuration);
                entity.UnitQty = updated.UnitQty;
                entity.BasePrice = updated.BasePrice;
                entity.TarifAdd = updated.TarifAdd;
                entity.KmPer25 = updated.KmPer25;
                entity.OperatorCost = updated.OperatorCost;
                entity.Quantity = updated.Quantity;
                entity.Revenue = updated.Revenue;

                revenueTotal += updated.Revenue;
            }

            return revenueTotal;
        }

        private static ProfitLossItem BuildItem(ProfitLossItemInputDto input, bool usesDuration)
        {
            return new ProfitLossItem
            {
                ProcOfferId = input.ProcOfferId,
                UnitQty = usesDuration ? input.QtyItems : input.Quantity,
                Quantity = usesDuration ? input.Quantity : null,
                Revenue = input.Revenue,
                ItemName = "",
            };
        }
    }
}
