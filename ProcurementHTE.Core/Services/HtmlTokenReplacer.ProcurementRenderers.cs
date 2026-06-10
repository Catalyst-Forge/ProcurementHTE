using System.Text;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateDetailsTable(ICollection<ProcDetail> details)
        {
            var sb = new StringBuilder();
            var no = 1;

            foreach (var detail in details)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{detail.ItemName ?? "-"}</td>");
                sb.AppendLine($"  <td class='text-center'>{detail.Quantity ?? 0}</td>");
                sb.AppendLine($"  <td class='text-center'>{detail.Unit ?? "-"}</td>");
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }

        private static string GenerateOffersTable(ICollection<ProcOffer> offers)
        {
            if (offers == null || offers.Count == 0)
                return "<tr><td colspan='4' class='text-center'>Tidak ada item penawaran</td></tr>";

            var sb = new StringBuilder();
            var no = 1;

            foreach (var offer in offers)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{offer.ItemPenawaran}</td>");
                sb.AppendLine($"  <td class='text-center'>{offer.Qty.ToString("N0", Id)}</td>");
                sb.AppendLine($"  <td class='text-center'>{offer.Unit}</td>");
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }

        private static string GenerateOffersList(ICollection<ProcOffer> offers)
        {
            var sb = new StringBuilder();

            foreach (var offer in offers)
            {
                sb.AppendLine("<ol class='sub-list'>");
                sb.AppendLine(
                    $"  <li class='mb-1'>{offer.Qty} ({offer.Qty.ToTerbilang()}) {offer.Unit} {offer.ItemPenawaran}</li>"
                );
                sb.AppendLine("</ol>");
            }

            return sb.ToString();
        }
    }
}
