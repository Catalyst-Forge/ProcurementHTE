using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateOfferTable(
            ProfitLoss pnl,
            Procurement proc,
            string? jobTypeName = null
        )
        {
            if (pnl.VendorOffers == null || pnl.VendorOffers.Count == 0)
                return "<p class='text-center'>Tidak ada penawaran vendor.</p>";

            var pnlItemsByOfferId = (pnl.Items ?? new List<ProfitLossItem>())
                .ToDictionary(i => i.ProcOfferId, i => i);
            var procOffersById = (proc.ProcOffers ?? new List<ProcOffer>())
                .ToDictionary(o => o.ProcOfferId, o => o);
            var sb = new StringBuilder();
            var tripLabel = jobTypeName switch
            {
                "StandBy" or "Sewa Unit" => "Durasi",
                "Moving" => "Qty Revenue",
                _ => "Trip",
            };

            foreach (var vendorGroup in pnl.VendorOffers.GroupBy(vo => vo.VendorId).OrderBy(g => g.First().Vendor.VendorName))
            {
                var rounds = vendorGroup.Select(vo => vo.Round).Distinct().OrderBy(r => r).ToList();
                if (rounds.Count == 0)
                    continue;

                var minRound = rounds.First();
                var maxRound = rounds.Last();
                var totalRoundCount = maxRound - minRound + 1;
                decimal grandTotal = 0m;

                sb.AppendLine("<div class='mb-4'>");
                sb.AppendLine($"  <div class='fw-semibold mb-1'>{vendorGroup.First().Vendor?.VendorName ?? "-"}</div>");
                sb.AppendLine("  <table class='table table-bordered table-sm align-middle border-black'>");
                AppendOfferTableHeader(sb, tripLabel, totalRoundCount, minRound, maxRound, jobTypeName);
                AppendOfferTableRows(
                    sb,
                    vendorGroup,
                    pnlItemsByOfferId,
                    procOffersById,
                    jobTypeName,
                    minRound,
                    totalRoundCount,
                    ref grandTotal
                );
                AppendOfferTableFooter(sb, totalRoundCount, jobTypeName, grandTotal);
                sb.AppendLine("  </table>");
                sb.AppendLine("</div>");
            }

            return sb.ToString();
        }

        private static void AppendOfferTableHeader(
            StringBuilder sb,
            string tripLabel,
            int totalRoundCount,
            int minRound,
            int maxRound,
            string? jobTypeName
        )
        {
            sb.AppendLine("    <thead>");
            sb.AppendLine("      <tr>");
            sb.AppendLine("        <th class='blue-header text-center' style='width: 1rem'>No</th>");
            sb.AppendLine("        <th class='blue-header' style='width: 12rem'>Item</th>");
            sb.AppendLine("        <th class='blue-header text-center' style='width: 3rem'>Qty Items</th>");
            sb.AppendLine("        <th class='blue-header text-center' style='width: 4rem'>Unit Items</th>");
            sb.AppendLine($"        <th class='blue-header text-center' style='width: 4rem'>{tripLabel}</th>");
            if (IsRevenueQuantityJob(jobTypeName))
                sb.AppendLine("        <th class='blue-header text-center' style='width: 4rem'>Unit Revenue</th>");

            sb.AppendLine("        <th class='blue-header text-center'>Harga awal</th>");
            for (var r = minRound + 1; r <= maxRound; r++)
            {
                var negoIndex = r - minRound;
                sb.AppendLine($"        <th class='blue-header text-center'>Harga Nego #{negoIndex}</th>");
            }

            _ = totalRoundCount;
            sb.AppendLine("        <th class='blue-header text-center'>Total</th>");
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </thead>");
            sb.AppendLine("    <tbody>");
        }

        private static void AppendOfferTableFooter(
            StringBuilder sb,
            int totalRoundCount,
            string? jobTypeName,
            decimal grandTotal
        )
        {
            sb.AppendLine("    </tbody>");
            sb.AppendLine("    <tfoot>");
            sb.AppendLine("      <tr>");
            var colspan = 5 + totalRoundCount + (IsRevenueQuantityJob(jobTypeName) ? 1 : 0);
            sb.AppendLine($"        <th colspan='{colspan}'>Tagihan</th>");
            sb.AppendLine($"        <th class='text-end'>{grandTotal.ToString("N0", Id)}</th>");
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </tfoot>");
        }

        private static bool IsRevenueQuantityJob(string? jobTypeName)
        {
            return jobTypeName == "Moving" || jobTypeName == "StandBy" || jobTypeName == "Sewa Unit";
        }
    }
}
