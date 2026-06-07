using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        private static void StampItemsWithProfitLossId(
            IEnumerable<ProfitLossItem> items,
            string profitLossId
        )
        {
            foreach (var item in items)
            {
                item.ProfitLossId = profitLossId;
            }
        }

        private static void StampOffersWithProfitLossId(
            IEnumerable<VendorOffer> offers,
            string profitLossId
        )
        {
            foreach (var offer in offers)
            {
                offer.ProfitLossId = profitLossId;
            }
        }

        private async Task UpdateProcOfferUnitRevenueAsync(List<ProfitLossItemInputDto> items)
        {
            if (items == null || items.Count == 0)
                return;

            foreach (var item in items)
            {
                if (
                    string.IsNullOrWhiteSpace(item.ProcOfferId)
                    || string.IsNullOrWhiteSpace(item.UnitRevenue)
                )
                    continue;

                await _pnlRepository.UpdateProcOfferUnitRevenueAsync(
                    item.ProcOfferId,
                    item.UnitRevenue
                );
            }
        }

        private async Task UpdateRoundLettersAsync(
            string procurementId,
            string profitLossId,
            List<VendorItemOffersDto> vendors
        )
        {
            if (vendors == null || vendors.Count == 0)
                return;

            foreach (var vendor in vendors)
            {
                if (vendor == null || string.IsNullOrWhiteSpace(vendor.VendorId))
                    continue;

                var letters = vendor.Letters ?? [];
                for (int i = 0; i < letters.Count; i++)
                {
                    await _roundLetterRepository.UpdateProfitLossLinkAsync(
                        procurementId,
                        vendor.VendorId,
                        i + 1,
                        profitLossId,
                        letters[i]
                    );
                }
            }

            await _roundLetterRepository.SaveChangesAsync();
        }
    }
}
