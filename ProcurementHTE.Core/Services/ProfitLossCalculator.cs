using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public static class ProfitLossCalculator
    {
        public static (
            string vendorId,
            decimal totalFinal,
            Dictionary<string, decimal> finalPerItem
        ) PickBestVendor(
            IReadOnlyCollection<VendorOffer>? offers,
            IEnumerable<string>? procOfferIds
        )
        {
            if (offers == null || offers.Count == 0)
                throw new InvalidOperationException(
                    "Belum ada penawaran vendor yang bisa dihitung."
                );

            var requiredIds = procOfferIds?.Distinct().ToList() ?? [];
            var perVendorFull = BuildVendorItemCosts(offers)
                .Select(x => new
                {
                    x.VendorId,
                    Total = ComputeRequiredTotal(x.FinalPerItem, requiredIds),
                    x.FinalPerItem,
                })
                .Where(x => x.Total < decimal.MaxValue)
                .OrderBy(x => x.Total)
                .FirstOrDefault();

            if (perVendorFull != null)
                return (perVendorFull.VendorId, perVendorFull.Total, perVendorFull.FinalPerItem);

            var perVendorLoose =
                BuildVendorItemCosts(offers)
                    .Select(x => new
                    {
                        x.VendorId,
                        Total = SafeSum(x.FinalPerItem.Values),
                        x.FinalPerItem,
                    })
                    .OrderBy(x => x.Total)
                    .FirstOrDefault()
                ?? throw new InvalidOperationException("Belum ada penawaran vendor yang valid.");

            return (perVendorLoose.VendorId, perVendorLoose.Total, perVendorLoose.FinalPerItem);
        }

        public static decimal ComputeRequiredTotal(
            IReadOnlyDictionary<string, decimal> finalPerItem,
            IReadOnlyCollection<string> requiredIds
        )
        {
            if (requiredIds.Count == 0)
                return SafeSum(finalPerItem.Values);

            decimal total = 0m;
            foreach (var id in requiredIds)
            {
                if (!finalPerItem.TryGetValue(id, out var perItem))
                    return decimal.MaxValue;

                total = SafeAdd(total, perItem);
                if (total == decimal.MaxValue)
                    return decimal.MaxValue;
            }

            return total;
        }

        public static decimal SafeSum(IEnumerable<decimal> values)
        {
            decimal total = 0m;
            foreach (var value in values)
            {
                total = SafeAdd(total, value);
                if (total == decimal.MaxValue)
                    return decimal.MaxValue;
            }

            return total;
        }

        public static decimal ComputeVendorItemCost(decimal price, decimal quantity, decimal trip)
        {
            var safeQuantity = quantity <= 0 ? 1 : quantity;
            var safeTrip = trip <= 0 ? 1 : trip;

            return price * safeQuantity * safeTrip;
        }

        private static IEnumerable<VendorCostRow> BuildVendorItemCosts(
            IEnumerable<VendorOffer> offers
        )
        {
            return offers.GroupBy(offer => offer.VendorId)
                .Select(group => new VendorCostRow(
                    group.Key,
                    group.GroupBy(x => x.ProcOfferId)
                        .ToDictionary(
                            gg => gg.Key,
                            gg =>
                            {
                                var ordered = gg.OrderBy(x => x.Round).ToList();
                                var last = ordered.Last();
                                var minPrice = ordered.Min(x => x.Price);
                                return ComputeVendorItemCost(
                                    minPrice,
                                    last.QuantityItem,
                                    last.QuantityOfUnit
                                );
                            }
                        )
                ));
        }

        private static decimal SafeAdd(decimal current, decimal addition)
        {
            if (current == decimal.MaxValue || addition == decimal.MaxValue)
                return decimal.MaxValue;

            if (addition >= 0m)
            {
                if (decimal.MaxValue - current <= addition)
                    return decimal.MaxValue;
            }
            else
            {
                if (decimal.MinValue - current >= addition)
                    return decimal.MinValue;
            }

            return current + addition;
        }

        private sealed record VendorCostRow(
            string VendorId,
            Dictionary<string, decimal> FinalPerItem
        );
    }
}
