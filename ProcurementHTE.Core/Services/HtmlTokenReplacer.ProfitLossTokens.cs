using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private async Task<(decimal? AccrualAmount, decimal? RealizationAmount)>
            ApplyProfitLossTokensAsync(HtmlTokenReplacementContext context, ProfitLoss pnl)
        {
            var items = pnl.Items?.ToList() ?? [];
            var revenueTotal = items.Sum(i => i.Revenue);
            var accrualAmount = pnl.AccrualAmount ?? revenueTotal;
            var realizationAmount = pnl.RealizationAmount;

            ApplyProfitLossDocumentTokens(context, pnl, revenueTotal);
            ApplyProfitLossItemsTokens(context, pnl);
            ApplyProfitLossVendorOffersTokens(context, pnl, revenueTotal);
            await ApplySelectedVendorTokensAsync(context, pnl);
            context.Replace("PnlEstimateTable", GeneratePnlEstimateTable(pnl, context.Procurement, context.JobTypeName));

            return (accrualAmount, realizationAmount);
        }

        private static void ApplyProfitLossDocumentTokens(
            HtmlTokenReplacementContext context,
            ProfitLoss pnl,
            decimal revenueTotal
        )
        {
            context.Replace("SelectedVendorFinalOffer", FormatDecimal(pnl.SelectedVendorFinalOffer));
            context.Replace(
                "SelectedVendorFinalOfferTerbilang",
                pnl.SelectedVendorFinalOffer.ToTerbilangRupiah()
            );
            context.Replace("Profit", FormatDecimal(pnl.Profit));
            context.Replace("ProfitPercent", pnl.ProfitPercent.ToString("N2", Id));
            context.Replace("Distance", FormatDecimal(pnl.Distance));
            context.Replace("PnlCreatedAt", FormatDate(pnl.CreatedAt));
            context.Replace("PnlUpdatedAt", FormatDate(pnl.UpdatedAt));
            context.Replace("TotalRevenue", FormatCurrency(revenueTotal));
            context.Replace("RevenueTerbilang", revenueTotal.ToTerbilangRupiah());
            context.Replace("NoLetter", pnl.NoLetterSelectedVendor);
            context.Replace(
                "JustifikasiListItem",
                pnl.SelectedVendorFinalOffer > 300_000_000m ? "<li>Justifikasi</li>" : string.Empty
            );
            context.Replace("TglTerimaWO", FormatDate(context.Procurement.StartDate));
            context.Replace(
                "SidebarAdditionalRows",
                GenerateSidebarAdditionalRows(
                    context.JobTypeName,
                    pnl,
                    FormatDate,
                    FormatDecimal
                )
            );
            context.Replace("TglMulaiSewa", FormatDate(pnl.TglMulaiSewa));
            context.Replace("TglMulaiMoving", FormatDate(pnl.TglMulaiMoving));
            context.Replace(
                "RincianSanksiRKS",
                pnl.SelectedVendorFinalOffer < 300_000_000m
                    ? "Jumlah denda sebesar 1‰ (satu mil) dari total Nilai Kontrak untuk setiap hari keterlambatan dengan maksimum denda sebesar 5% (lima persen)"
                    : "Jumlah denda sebesar 1% (satu persen) dari total Nilai Kontrak untuk setiap hari keterlambatan dengan maksimum denda sebesar 5% (lima persen)"
            );
        }

        private static void ApplyProfitLossItemsTokens(
            HtmlTokenReplacementContext context,
            ProfitLoss pnl
        )
        {
            context.Replace("PnlItemsTableHeader", GenerateItemsTableHeader(context.JobTypeName));
            context.Replace(
                "PnlItemsTableColspan",
                GetItemsTableColspan(context.JobTypeName).ToString()
            );

            if (pnl.Items != null && pnl.Items.Count != 0)
            {
                context.Replace(
                    "PnlItemsTable",
                    GenerateItemsTable(pnl.Items, context.TemplateKey, context.JobTypeName)
                );
            }
        }

        private static void ApplyProfitLossVendorOffersTokens(
            HtmlTokenReplacementContext context,
            ProfitLoss pnl,
            decimal revenueTotal
        )
        {
            if (pnl.VendorOffers != null && pnl.VendorOffers.Count != 0)
            {
                context.Replace(
                    "VendorOfferTable",
                    GenerateOfferTable(pnl, context.Procurement, context.JobTypeName)
                );
                context.Replace(
                    "VendorNegotiationTable",
                    GenerateVendorNegotiationTable(pnl, context.Procurement, revenueTotal)
                );
                ApplyRoundTokens(context, pnl);
                return;
            }

            context.Replace("VendorOfferTable", "<p class='text-center'>Tidak ada penawaran vendor</p>");
            context.Replace(
                "VendorNegotiationTable",
                "<tr><td colspan='4' class='text-center'>Tidak ada penawaran vendor</td></tr>"
            );
            context.Replace("Round", "-");
            context.Replace("RoundCreatedAt", "-");
        }

        private static void ApplyRoundTokens(HtmlTokenReplacementContext context, ProfitLoss pnl)
        {
            var highestRound = pnl.VendorOffers.Max(vo => vo.Round);
            var highestRoundDate = pnl
                .VendorOffers.Where(vo => vo.Round == highestRound)
                .OrderByDescending(vo => vo.CreatedAt)
                .FirstOrDefault()
                ?.CreatedAt;

            context.Replace("Round", highestRound > 0 ? highestRound.ToString("N0", Id) : "-");
            context.Replace(
                "RoundCreatedAt",
                highestRoundDate.HasValue ? FormatDate(highestRoundDate) : "-"
            );
        }
    }
}
