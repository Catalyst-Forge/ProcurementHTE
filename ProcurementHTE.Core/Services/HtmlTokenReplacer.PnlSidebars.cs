using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateSidebarAdditionalRows(
            string? jobTypeName,
            ProfitLoss pnl,
            Func<DateTime?, string> formatDate,
            Func<decimal?, string, string> formatDecimal
        )
        {
            var sb = new StringBuilder();

            if (jobTypeName == "StandBy" || jobTypeName == "Sewa Unit")
            {
                sb.AppendLine("<tr>");
                sb.AppendLine("  <td>Tanggal Mulai Sewa</td>");
                sb.AppendLine($"  <td class='text-end'>{formatDate(pnl.TglMulaiSewa)}</td>");
                sb.AppendLine("</tr>");
                return sb.ToString();
            }

            if (jobTypeName == "Moving")
            {
                sb.AppendLine("<tr>");
                sb.AppendLine("  <td>Tgl Mulai Moving</td>");
                sb.AppendLine($"  <td class='text-end'>{formatDate(pnl.TglMulaiMoving)}</td>");
                sb.AppendLine("</tr>");
                sb.AppendLine("<tr>");
                sb.AppendLine("  <td>Jarak</td>");
                sb.AppendLine($"  <td class='text-end'>{formatDecimal(pnl.Distance, "N0")} Km</td>");
                sb.AppendLine("</tr>");
                return sb.ToString();
            }

            sb.AppendLine("<tr>");
            sb.AppendLine("  <td>Jarak</td>");
            sb.AppendLine($"  <td class='text-end'>{formatDecimal(pnl.Distance, "N0")} KM</td>");
            sb.AppendLine("</tr>");
            return sb.ToString();
        }

        private static string GenerateVendorNameList(ICollection<Vendor> vendorList)
        {
            if (vendorList == null || vendorList.Count == 0)
                return "<p class='text-muted mb-0'>Tidak ada vendor.</p>";

            var sb = new StringBuilder();
            sb.AppendLine("<div class='vendor-list mb-3'>");

            foreach (var item in vendorList.Select((v, i) => new { No = i + 1, Name = v.VendorName ?? "-" }))
            {
                sb.AppendLine("  <div class='vendor-item'>");
                sb.AppendLine($"    <div class='vendor-item-no'>{item.No}</div>");
                sb.AppendLine($"    <div class='vendor-item-name'>{item.Name}</div>");
                sb.AppendLine("  </div>");
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }
    }
}
