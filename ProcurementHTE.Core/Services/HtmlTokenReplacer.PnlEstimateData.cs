using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static List<PnlVendorEstimate> BuildPnlVendorEstimates(
            IReadOnlyCollection<ProfitLossItem> items,
            IReadOnlyCollection<VendorOffer> vendorOffers,
            decimal revenueTotal,
            string? jobTypeName
        )
        {
            var pnlItemsByOfferId = items.ToDictionary(i => i.ProcOfferId, i => i);
            var vendors = new List<PnlVendorEstimate>();

            foreach (var vendorId in vendorOffers.Select(vo => vo.VendorId).Distinct())
            {
                var offersForVendor = vendorOffers.Where(vo => vo.VendorId == vendorId).ToList();
                var bestTotal = FindBestVendorRoundTotal(
                    offersForVendor,
                    pnlItemsByOfferId,
                    jobTypeName
                );
                if (bestTotal == null)
                    continue;

                var vendorTotal = bestTotal.Value;
                var profit = revenueTotal - vendorTotal;
                var profitPercent =
                    revenueTotal > 0
                        ? Math.Round(((revenueTotal - vendorTotal) / revenueTotal) * 100m, 2)
                        : 0m;

                vendors.Add(new PnlVendorEstimate(
                    offersForVendor.First().Vendor,
                    vendorTotal,
                    profit,
                    profitPercent
                ));
            }

            return vendors;
        }

        private static decimal? FindBestVendorRoundTotal(
            IReadOnlyCollection<VendorOffer> offersForVendor,
            IReadOnlyDictionary<string, ProfitLossItem> pnlItemsByOfferId,
            string? jobTypeName
        )
        {
            decimal? bestTotal = null;
            int? bestRound = null;
            var itemGroups = offersForVendor.GroupBy(vo => vo.ProcOfferId).ToList();
            var rounds = offersForVendor.Select(vo => vo.Round).Distinct().OrderBy(r => r).ToList();

            foreach (var round in rounds)
            {
                var roundTotal = CalculateVendorRoundTotal(
                    round,
                    itemGroups,
                    pnlItemsByOfferId,
                    jobTypeName,
                    out var hasRow
                );
                if (!hasRow)
                    continue;

                if (
                    bestTotal == null
                    || roundTotal < bestTotal
                    || (roundTotal == bestTotal && (bestRound == null || round > bestRound))
                )
                {
                    bestTotal = roundTotal;
                    bestRound = round;
                }
            }

            return bestTotal;
        }

        private static decimal CalculateVendorRoundTotal(
            int round,
            IEnumerable<IGrouping<string, VendorOffer>> itemGroups,
            IReadOnlyDictionary<string, ProfitLossItem> pnlItemsByOfferId,
            string? jobTypeName,
            out bool hasRow
        )
        {
            decimal roundTotal = 0m;
            hasRow = false;

            foreach (var itemGroup in itemGroups)
            {
                var offerForRound = itemGroup.FirstOrDefault(o => o.Round == round);
                if (offerForRound == null || offerForRound.Price <= 0)
                    continue;

                pnlItemsByOfferId.TryGetValue(itemGroup.Key, out var pnlItem);
                var qty = pnlItem?.UnitQty ?? offerForRound.QuantityItem;
                if (qty <= 0)
                    continue;

                var trip = IsRevenueQuantityJob(jobTypeName)
                    ? pnlItem?.Quantity ?? offerForRound.QuantityOfUnit
                    : offerForRound.QuantityOfUnit;
                roundTotal += offerForRound.Price * qty * (trip > 0 ? trip : 1);
                hasRow = true;
            }

            return roundTotal;
        }

        private static void AppendPnlEstimateBlankRows(
            StringBuilder sb,
            IReadOnlyCollection<PnlVendorEstimate> vendors
        )
        {
            AppendPnlEstimateBlankRow(sb, vendors, "Adjustment naik 15%", textCenter: false);
            AppendPnlEstimateBlankRow(sb, vendors, "Potential new Profit", textCenter: false);
            AppendPnlEstimateBlankRow(sb, vendors, "% Profit Rev", textCenter: true);
        }

        private static void AppendPnlEstimateBlankRow(
            StringBuilder sb,
            IReadOnlyCollection<PnlVendorEstimate> vendors,
            string label,
            bool textCenter
        )
        {
            sb.AppendLine(textCenter ? "    <tr class='text-center'>" : "    <tr>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine(textCenter ? $"      <td>{label}</td>" : $"      <td class='text-end'>{label}</td>");
            foreach (var _ in vendors)
                sb.AppendLine("      <td class='text-end'>&ThinSpace;</td>");
            sb.AppendLine("    </tr>");
        }

        private sealed record PnlVendorEstimate(
            Vendor Vendor,
            decimal Total,
            decimal Profit,
            decimal ProfitPercent
        );
    }
}
