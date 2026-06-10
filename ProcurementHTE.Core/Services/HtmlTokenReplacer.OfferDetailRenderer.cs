using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateOfferDetailTable(
            ProfitLoss pnl,
            Procurement proc,
            out decimal selectedVendorTotal
        )
        {
            selectedVendorTotal = 0m;

            if (pnl.VendorOffers == null || pnl.VendorOffers.Count == 0)
                return "<tr><td colspan='6' class='text-center'>Tidak ada penawaran vendor</td></tr>";

            var selectedVendorId = !string.IsNullOrWhiteSpace(pnl.SelectedVendorId)
                ? pnl.SelectedVendorId
                : pnl.VendorOffers.GroupBy(o => o.VendorId).OrderBy(g => g.Key).First().Key;
            var offersForSelectedVendor = pnl
                .VendorOffers.Where(o => o.VendorId == selectedVendorId)
                .ToList();

            if (offersForSelectedVendor.Count == 0)
                return "<tr><td colspan='6' class='text-center'>Tidak ada penawaran vendor</td></tr>";

            var sb = new StringBuilder();
            var no = 1;
            var procOffers = proc.ProcOffers?.ToDictionary(o => o.ProcOfferId, o => o) ?? [];
            var pnlItems = pnl.Items?.ToDictionary(i => i.ProcOfferId, i => i) ?? [];
            var itemGroups = offersForSelectedVendor
                .GroupBy(o => o.ProcOfferId)
                .OrderBy(g => g.First().ProcOffer.ItemPenawaran);

            foreach (var group in itemGroups)
            {
                var offer = group
                    .Where(o => o.Round == group.Max(x => x.Round) && o.Price > 0)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();
                if (offer == null)
                    continue;

                procOffers.TryGetValue(group.Key, out var procOffer);
                pnlItems.TryGetValue(group.Key, out var pnlItem);
                var qty = pnlItem?.UnitQty ?? offer.QuantityItem;
                var trip = offer.QuantityOfUnit > 0 ? offer.QuantityOfUnit : 1;
                var total = offer.Price * qty * trip;
                selectedVendorTotal += total;

                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{procOffer?.ItemPenawaran ?? "-"}</td>");
                sb.AppendLine($"  <td class='text-center'>{qty}</td>");
                sb.AppendLine($"  <td class='text-center'>{procOffer?.Unit ?? offer.ProcOffer?.Unit ?? "-"}</td>");
                sb.AppendLine($"  <td class='text-end'>{offer.Price.ToString("C0", Id)}</td>");
                sb.AppendLine($"  <td class='text-end'>{total.ToString("C0", Id)}</td>");
                sb.AppendLine("</tr>");
            }

            return selectedVendorTotal == 0m && sb.Length == 0
                ? "<tr><td colspan='6' class='text-center'>Tidak ada penawaran vendor</td></tr>"
                : sb.ToString();
        }
    }
}
