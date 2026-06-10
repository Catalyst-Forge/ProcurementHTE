using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GeneratePnlEstimateTable(
            ProfitLoss pnl,
            Procurement proc,
            string? jobTypeName = null
        )
        {
            _ = proc;

            var items = pnl.Items?.ToList() ?? [];
            if (items.Count == 0)
                return "<p class='text-center'>Tidak ada data Profit &amp; Loss.</p>";

            var vendorOffers = pnl.VendorOffers?.ToList() ?? [];
            if (vendorOffers.Count == 0)
                return "<p class='text-center'>Tidak ada penawaran vendor untuk Profit &amp; Loss summary.</p>\r\n";

            var revenueTotal = items.Sum(i => i.Revenue);
            var vendors = BuildPnlVendorEstimates(items, vendorOffers, revenueTotal, jobTypeName);
            if (vendors.Count == 0)
                return "<p class='text-center'>Tidak ada data penawaran yang dapat dihitung.</p>";

            vendors = vendors.OrderBy(v => v.Vendor.VendorName).ToList();
            var best = vendors
                .OrderByDescending(v => v.Profit)
                .ThenByDescending(v => v.ProfitPercent)
                .ThenBy(v => v.Total)
                .First();

            return RenderPnlEstimateTable(vendors, best, revenueTotal);
        }

        private static string RenderPnlEstimateTable(
            IReadOnlyCollection<PnlVendorEstimate> vendors,
            PnlVendorEstimate best,
            decimal revenueTotal
        )
        {
            var sb = new StringBuilder();
            var vendorCount = vendors.Count;
            var totalColumns = 2 + vendorCount;

            sb.AppendLine("<table class='table table-bordered table-sm align-middle mb-3 border-black'>");
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            sb.AppendLine($"      <th colspan='{totalColumns}' class='green-header text-center'>Profit &amp; Loss Estimate</th>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <th class='green-header' style='width: 1rem'></th>");
            sb.AppendLine("      <th class='green-header' style='width: 20rem'>TAGIHAN PDSI</th>");
            sb.AppendLine($"      <th colspan='{vendorCount}' class='green-header'>PENAWARAN MITRA</th>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");
            sb.AppendLine("  <tbody>");
            AppendPnlEstimateRevenueRows(sb, vendors, revenueTotal, totalColumns);
            AppendPnlEstimateProfitRows(sb, vendors, best);
            AppendPnlEstimateBlankRows(sb, vendors);
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        private static void AppendPnlEstimateRevenueRows(
            StringBuilder sb,
            IReadOnlyCollection<PnlVendorEstimate> vendors,
            decimal revenueTotal,
            int totalColumns
        )
        {
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine("      <td>Revenue</td>");
            foreach (var v in vendors)
                sb.AppendLine($"      <td style='width: 17rem'>{v.Vendor.VendorName ?? "-"}</td>");
            sb.AppendLine("    </tr>");

            sb.AppendLine("    <tr>");
            sb.AppendLine("      <td>Rp</td>");
            sb.AppendLine($"      <td class='text-end'>{revenueTotal.ToString("N0", Id)}</td>");
            foreach (var v in vendors)
                sb.AppendLine($"      <td class='text-end'>{v.Total.ToString("N0", Id)}</td>");
            sb.AppendLine("    </tr>");

            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine($"      <td colspan='{totalColumns - 2}' class='fw-semibold'>COST OPERATOR</td>");
            foreach (var _ in vendors)
                sb.AppendLine("      <td></td>");
            sb.AppendLine("    </tr>");
        }

        private static void AppendPnlEstimateProfitRows(
            StringBuilder sb,
            IReadOnlyCollection<PnlVendorEstimate> vendors,
            PnlVendorEstimate best
        )
        {
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td colspan='2' class='fw-semibold'>PROFIT</td>");
            foreach (var v in vendors)
            {
                var css = v.Vendor.VendorId == best.Vendor.VendorId
                    ? "green-highlight text-end"
                    : "text-end";
                sb.AppendLine($"      <td class='{css}'>Rp {v.Profit.ToString("N0", Id)}</td>");
            }
            sb.AppendLine("    </tr>");

            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td colspan='2' class='fw-semibold'>%</td>");
            foreach (var v in vendors)
            {
                var css = v.Vendor.VendorId == best.Vendor.VendorId
                    ? "green-highlight text-end"
                    : "text-end";
                sb.AppendLine($"      <td class='{css}'>{v.ProfitPercent.ToString("N2", Id)}%</td>");
            }
            sb.AppendLine("    </tr>");
        }
    }
}
