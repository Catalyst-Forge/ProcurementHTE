using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateVendorNegotiationTable(
            ProfitLoss pnl,
            Procurement proc,
            decimal revenueTotal
        )
        {
            _ = proc;

            if (pnl.VendorOffers == null || pnl.VendorOffers.Count == 0)
                return "<tr><td colspan='4' class='text-center'>Tidak ada penawaran vendor</td></tr>";

            var pnlItems = pnl.Items ?? new List<ProfitLossItem>();
            var pnlItemsByOfferId = pnlItems.ToDictionary(i => i.ProcOfferId, i => i);
            var rows = new StringBuilder();
            var vendorGroups = pnl
                .VendorOffers.GroupBy(o => o.VendorId)
                .OrderBy(g => g.First().Vendor.VendorName);

            foreach (var vendorGroup in vendorGroups)
            {
                var firstOfferTotal = CalcTotalForRound(vendorGroup, vendorGroup.Min(o => o.Round), pnlItemsByOfferId);
                var negoTotal = CalcTotalForRound(vendorGroup, vendorGroup.Max(o => o.Round), pnlItemsByOfferId);

                if (firstOfferTotal == 0 && negoTotal == 0)
                    continue;

                rows.AppendLine("<tr>");
                rows.AppendLine($"  <td>{vendorGroup.First().Vendor?.VendorName ?? "-"}</td>");
                rows.AppendLine($"  <td class='text-end'>{firstOfferTotal.ToString("C0", Id)}</td>");
                rows.AppendLine($"  <td class='text-end'>{negoTotal.ToString("C0", Id)}</td>");
                rows.AppendLine($"  <td>{BuildNegotiationRemark(revenueTotal, negoTotal)}</td>");
                rows.AppendLine("</tr>");
            }

            return rows.Length > 0
                ? rows.ToString()
                : "<tr><td colspan='4' class='text-center'>Tidak ada penawaran vendor</td></tr>";
        }

        private static decimal CalcTotalForRound(
            IEnumerable<VendorOffer> offers,
            int round,
            IReadOnlyDictionary<string, ProfitLossItem> pnlItemsByOfferId
        )
        {
            decimal total = 0m;
            var hasRow = false;

            foreach (var group in offers.GroupBy(o => o.ProcOfferId))
            {
                var offer = group.FirstOrDefault(o => o.Round == round);
                if (offer == null || offer.Price <= 0)
                    continue;

                pnlItemsByOfferId.TryGetValue(group.Key, out var pnlItem);
                var qty = pnlItem?.UnitQty ?? offer.QuantityItem;
                if (qty <= 0)
                    continue;

                var trip = offer.QuantityOfUnit > 0 ? offer.QuantityOfUnit : 1;
                total += offer.Price * qty * trip;
                hasRow = true;
            }

            return hasRow ? total : 0m;
        }

        private static string BuildNegotiationRemark(decimal revenueTotal, decimal negoTotal)
        {
            if (revenueTotal <= 0 || negoTotal <= 0)
                return "-";

            var profitPercent = Math.Round(((revenueTotal - negoTotal) / revenueTotal) * 100m, 2);
            return $"PROFIT {profitPercent.ToString("N2", Id)}%";
        }
    }
}
