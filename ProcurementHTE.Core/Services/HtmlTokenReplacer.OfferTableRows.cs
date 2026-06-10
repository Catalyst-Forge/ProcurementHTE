using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static void AppendOfferTableRows(
            StringBuilder sb,
            IEnumerable<VendorOffer> vendorGroup,
            IReadOnlyDictionary<string, ProfitLossItem> pnlItemsByOfferId,
            IReadOnlyDictionary<string, ProcOffer> procOffersById,
            string? jobTypeName,
            int minRound,
            int totalRoundCount,
            ref decimal grandTotal
        )
        {
            var no = 1;
            var itemGroups = vendorGroup
                .GroupBy(vo => vo.ProcOfferId)
                .OrderBy(g => g.First().ProcOffer.ItemPenawaran);

            foreach (var itemGroup in itemGroups)
            {
                pnlItemsByOfferId.TryGetValue(itemGroup.Key, out var pnlItem);
                procOffersById.TryGetValue(itemGroup.Key, out var baseOffer);

                var firstOfferForItem = itemGroup.OrderBy(vo => vo.Round).First();
                var qty = pnlItem?.UnitQty ?? baseOffer?.Qty ?? 0;
                var trip = IsRevenueQuantityJob(jobTypeName)
                    ? pnlItem?.Quantity ?? firstOfferForItem.QuantityOfUnit
                    : firstOfferForItem.QuantityOfUnit;
                var pricesPerRound = BuildPricesPerRound(itemGroup, minRound, totalRoundCount);
                var finalPrice = PickFinalPrice(itemGroup);
                decimal? total = null;

                if (finalPrice.HasValue && qty > 0 && trip > 0)
                {
                    total = finalPrice.Value * qty * trip;
                    grandTotal += total.Value;
                }

                sb.AppendLine("      <tr>");
                sb.AppendLine($"        <td class='text-center'>{no++}</td>");
                sb.AppendLine($"        <td>{baseOffer?.ItemPenawaran ?? pnlItem?.ProcOffer?.ItemPenawaran ?? "-"}</td>");
                sb.AppendLine($"        <td class='text-center'>{qty.ToString("N0", Id)}</td>");
                sb.AppendLine($"        <td class='text-center'>{baseOffer?.Unit ?? "-"}</td>");
                sb.AppendLine($"        <td class='text-center'>{trip.ToString("0.##", Id)}</td>");
                if (IsRevenueQuantityJob(jobTypeName))
                {
                    var unitRevenue = baseOffer?.UnitRevenue ?? pnlItem?.ProcOffer?.UnitRevenue ?? "-";
                    sb.AppendLine($"        <td class='text-center'>{unitRevenue}</td>");
                }

                foreach (var price in pricesPerRound)
                {
                    sb.AppendLine(
                        price.HasValue
                            ? $"        <td class='text-end'>{price.Value.ToString("N0", Id)}</td>"
                            : "        <td class='text-center'>No Quote</td>"
                    );
                }

                sb.AppendLine($"        <td class='text-end'>{(total.HasValue ? total.Value.ToString("N0", Id) : "-")}</td>");
                sb.AppendLine("      </tr>");
            }
        }

        private static decimal?[] BuildPricesPerRound(
            IEnumerable<VendorOffer> itemGroup,
            int minRound,
            int totalRoundCount
        )
        {
            var pricesPerRound = new decimal?[totalRoundCount];
            foreach (var offer in itemGroup)
            {
                var index = offer.Round - minRound;
                if (index >= 0 && index < totalRoundCount)
                    pricesPerRound[index] = offer.Price;
            }

            return pricesPerRound;
        }

        private static decimal? PickFinalPrice(IEnumerable<VendorOffer> itemGroup)
        {
            decimal? finalPrice = null;
            int? finalRound = null;

            foreach (var offer in itemGroup)
            {
                if (offer.Price <= 0)
                    continue;

                if (
                    finalPrice == null
                    || offer.Price < finalPrice
                    || (offer.Price == finalPrice && (finalRound == null || offer.Round > finalRound))
                )
                {
                    finalPrice = offer.Price;
                    finalRound = offer.Round;
                }
            }

            return finalPrice;
        }
    }
}
