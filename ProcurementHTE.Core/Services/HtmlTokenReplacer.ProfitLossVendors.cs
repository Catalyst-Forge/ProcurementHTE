using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private async Task ApplySelectedVendorTokensAsync(
            HtmlTokenReplacementContext context,
            ProfitLoss pnl
        )
        {
            if (string.IsNullOrEmpty(pnl.SelectedVendorId))
            {
                ApplySelectedVendorFallbackTokens(context);
                return;
            }

            var allVendor = await _vendorRepo.GetAllAsync();
            var selectVendor = await _pnlRepo.GetSelectedVendorsAsync(pnl.ProcurementId);
            var participatingVendors = selectVendor
                .Select(row => allVendor.FirstOrDefault(v => v.VendorId == row.VendorId))
                .Where(v => v != null)
                .Cast<Vendor>()
                .ToList();

            context.Replace("VendorList", GenerateVendorNameList(participatingVendors));
            context.Replace("SelectedVendorCount", participatingVendors.Count.ToString());
            context.Replace("VendorCount", allVendor.Count().ToString());
            context.Replace("SelectedVendorCountTerbilang", participatingVendors.Count.ToTerbilang());

            var vendor = await _vendorRepo.GetByIdAsync(pnl.SelectedVendorId);
            if (vendor == null)
                return;

            context.Replace("SelectedVendorName", vendor.VendorName);
            context.Replace("SelectedVendorNPWP", vendor.NPWP);
            context.Replace("SelectedVendorAddress", vendor.Address);
            context.Replace("SelectedVendorCity", vendor.City);
            context.Replace("SelectedVendorProvince", vendor.Province);
            context.Replace("SelectedVendorEmail", vendor.Email);
        }

        private static void ApplySelectedVendorFallbackTokens(HtmlTokenReplacementContext context)
        {
            context.Replace("SelectedVendorName", "-");
            context.Replace("SelectedVendorNPWP", "-");
            context.Replace("SelectedVendorAddress", "-");
            context.Replace("SelectedVendorCity", "-");
            context.Replace("SelectedVendorProvince", "-");
            context.Replace("SelectedVendorEmail", "-");
        }

        private static void ApplyProfitLossFallbackTokens(HtmlTokenReplacementContext context)
        {
            context.Replace("QuantityTotal", "-");
            context.Replace("TarifAwal", "-");
            context.Replace("TarifAdd", "-");
            context.Replace("KmPer25", "-");
            context.Replace("OperatorCost", "-");
            context.Replace("Revenue", "-");
            context.Replace("Distance", "-");
            context.Replace("SelectedVendorFinalOffer", "-");
            context.Replace("Profit", "-");
            context.Replace("ProfitPercent", "-");
            context.Replace("PnlCreatedAt", "-");
            context.Replace("PnlUpdatedAt", "-");
            ApplySelectedVendorFallbackTokens(context);
            context.Replace("JustifikasiListItem", string.Empty);
            context.Replace("Round", "-");
            context.Replace("RoundCreatedAt", "-");
            context.Replace(
                "VendorNegotiationTable",
                "<tr><td colspan='4' class='text-center'>Tidak ada penawaran vendor</td></tr>"
            );
        }

        private static void ApplyOfferDetailTokens(
            HtmlTokenReplacementContext context,
            ProfitLoss? pnl
        )
        {
            if (pnl == null)
            {
                context.Replace(
                    "OfferDetailTable",
                    "<tr><td colspan='6' class='text-center'>Tidak ada penawaran vendor</td></tr>"
                );
                context.Replace("SelectedVendorOfferTotal", "-");
                context.Replace("SelectedVendorOfferTotalTerbilang", "-");
                return;
            }

            var offerDetailTable = GenerateOfferDetailTable(
                pnl,
                context.Procurement,
                out var selectedVendorOfferTotal
            );
            context.Replace("OfferDetailTable", offerDetailTable);
            context.Replace(
                "SelectedVendorOfferTotal",
                selectedVendorOfferTotal > 0 ? selectedVendorOfferTotal.ToString("C0", Id) : "-"
            );
            context.Replace(
                "SelectedVendorOfferTotalTerbilang",
                selectedVendorOfferTotal > 0
                    ? selectedVendorOfferTotal.ToTerbilangRupiah()
                    : "-"
            );
        }
    }
}
