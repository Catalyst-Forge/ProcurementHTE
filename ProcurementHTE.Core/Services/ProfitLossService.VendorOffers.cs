using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        private List<VendorOffer> BuildVendorOffersMulti(
            List<VendorItemOffersDto> input,
            string procurementId,
            string jobTypeName
        )
        {
            var offers = new List<VendorOffer>();

            foreach (var vendor in input)
            {
                if (string.IsNullOrWhiteSpace(vendor.VendorId))
                    continue;

                var letters = vendor.Letters ?? [];
                foreach (var item in vendor.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ProcOfferId) || item.Quantity <= 0)
                        continue;

                    var unitTypeId = _jobTypeCalc.GetVendorOfferUnitTypeId(jobTypeName, null);
                    for (int i = 0; i < (item.Prices?.Count ?? 0); i++)
                    {
                        var price = item.Prices![i];
                        if (price <= 0)
                            continue;

                        var noLetter = letters.Count > i ? letters[i] : null;
                        offers.Add(
                            new VendorOffer
                            {
                                ProcurementId = procurementId,
                                VendorId = vendor.VendorId,
                                ProcOfferId = item.ProcOfferId,
                                Round = i + 1,
                                Price = price,
                                NoLetter = string.IsNullOrWhiteSpace(noLetter)
                                    ? string.Empty
                                    : noLetter!,
                                QuantityItem = item.Quantity,
                                QuantityOfUnit = item.Trip,
                                UnitTypeId = unitTypeId,
                            }
                        );
                    }
                }
            }

            return offers;
        }

        private static void ValidateVendorOffers(List<VendorOffer> offers)
        {
            if (offers.Count == 0)
                throw new InvalidOperationException(
                    "Minimal 1 vendor harus memberikan penawaran lengkap dengan harga, quantity, dan trip yang valid."
                );

            var invalidOffers = offers
                .Where(o =>
                    o.Price <= 0
                    || o.QuantityItem <= 0
                    || string.IsNullOrWhiteSpace(o.ProcOfferId)
                    || string.IsNullOrWhiteSpace(o.VendorId)
                )
                .ToList();

            if (invalidOffers.Any())
                throw new InvalidOperationException(
                    $"Terdapat {invalidOffers.Count} penawaran vendor yang tidak valid. "
                        + "Pastikan semua field (Harga, Quantity, Trip) terisi dengan benar."
                );
        }

        private static string? GetSelectedVendorLetter(
            IEnumerable<VendorOffer> offers,
            string? vendorId
        )
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return null;

            var best = offers
                .Where(o => o.VendorId == vendorId && !string.IsNullOrWhiteSpace(o.NoLetter))
                .OrderBy(o => o.Round)
                .LastOrDefault();

            return best?.NoLetter;
        }
    }
}
