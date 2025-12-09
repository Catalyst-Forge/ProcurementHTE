using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class ProfitLossService : IProfitLossService
    {
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IVendorOfferRepository _voRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IVendorRoundLetterRepository _roundLetterRepository;

        public ProfitLossService(
            IProfitLossRepository pnlRepository,
            IVendorOfferRepository voRepository,
            IVendorRepository vendorRepository,
            IVendorRoundLetterRepository roundLetterRepository
        )
        {
            _pnlRepository = pnlRepository;
            _voRepository = voRepository;
            _vendorRepository = vendorRepository;
            _roundLetterRepository = roundLetterRepository;
        }

        public Task<ProfitLoss?> GetByProcurementAsync(string procurementId)
        {
            return _pnlRepository.GetByProcurementAsync(procurementId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string procurementId)
        {
            return _pnlRepository.GetSelectedVendorsAsync(procurementId);
        }

        public async Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(string procurementId)
        {
            var pnl = await _pnlRepository.GetByProcurementAsync(procurementId);
            if (pnl == null)
                return null!;

            var allVendors = await _vendorRepository.GetAllAsync();
            var selectedRows = await _pnlRepository.GetSelectedVendorsAsync(procurementId);
            var offers = await _voRepository.GetByProcurementAsync(procurementId);

            var totalRevenue = SafeSum(pnl.Items.Select(item => item.Revenue));
            var totalOperatorCost = SafeSum(pnl.Items.Select(item => item.OperatorCost));

            var selectedVendorNames = selectedRows
                .Select(row =>
                    allVendors.FirstOrDefault(vendor => vendor.VendorId == row.VendorId)?.VendorName
                    ?? row.VendorId
                )
                .ToList();

            var requiredItemIds = pnl
                .Items.Select(item => item.ProcOfferId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var vendorComparisons = offers
                .GroupBy(vendorOffer => vendorOffer.VendorId)
                .Select(group =>
                {
                    var perItemCosts = group
                        .GroupBy(vendorOffer => vendorOffer.ProcOfferId)
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var ordered = g.OrderBy(vo => vo.Round).ToList();
                                var last = ordered.Last();
                                var minPrice = ordered.Min(vo => vo.Price);
                                return ComputeVendorItemCost(minPrice, last.Quantity, last.Trip);
                            },
                            StringComparer.OrdinalIgnoreCase
                        );

                    var requiredTotal = ComputeRequiredTotal(perItemCosts, requiredItemIds);
                    var finalOffer =
                        requiredTotal != decimal.MaxValue
                            ? requiredTotal
                            : SafeSum(perItemCosts.Values);

                    var vendorName =
                        allVendors
                            .FirstOrDefault(vendor => vendor.VendorId == group.Key)
                            ?.VendorName ?? group.Key;

                    var profit = totalRevenue - finalOffer;
                    var profitPercent = totalRevenue > 0 ? (profit / totalRevenue) * 100m : 0m;
                    var isSelected = pnl.SelectedVendorId == group.Key;

                    return new VendorComparisonDto
                    {
                        VendorName = vendorName,
                        FinalOffer = finalOffer,
                        Profit = profit,
                        ProfitPercent = profitPercent,
                        IsSelected = isSelected,
                    };
                })
                .OrderBy(row => row.FinalOffer)
                .ToList();

            var itemBreakdown = pnl
                .Items.Select(item =>
                {
                    var itemName =
                        pnl.Items.Select(i => i.ProcOffer)
                            .FirstOrDefault(i => i.ProcOfferId == item.ProcOfferId)
                            ?.ItemPenawaran
                        ?? item.ProcOfferId;

                    return (
                        item.ProcOfferId,
                        itemName,
                        item.Quantity,
                        item.TarifAwal,
                        item.TarifAdd,
                        item.KmPer25,
                        item.OperatorCost,
                        item.Revenue
                    );
                })
                .ToList();

            return new ProfitLossSummaryDto
            {
                ProfitLossId = pnl.ProfitLossId,
                ProcurementId = pnl.ProcurementId,
                TotalOperatorCost = totalOperatorCost,
                TotalRevenue = totalRevenue,
                AccrualAmount = pnl.AccrualAmount ?? totalRevenue,
                RealizationAmount = pnl.RealizationAmount ?? totalOperatorCost,
                Distance = pnl.Distance,
                SelectedVendorId = pnl.SelectedVendorId ?? "",
                SelectedVendorName =
                    vendorComparisons.FirstOrDefault(vendor => vendor.IsSelected)?.VendorName
                    ?? allVendors
                        .FirstOrDefault(vendor => vendor.VendorId == pnl.SelectedVendorId)
                        ?.VendorName
                    ?? pnl.SelectedVendorId,
                SelectedFinalOffer = pnl.SelectedVendorFinalOffer,
                Profit = pnl.Profit,
                ProfitPercent = pnl.ProfitPercent,
                Items = itemBreakdown,
                SelectedVendorNames = selectedVendorNames,
                VendorComparisons = vendorComparisons,
                CreatedAt = pnl.CreatedAt,
            };
        }

        public async Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId)
        {
            var pnl =
                await _pnlRepository.GetByIdAsync(profitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            var offers = await _voRepository.GetByProcurementAsync(pnl.ProcurementId);
            var roundLetters = await _roundLetterRepository.ListByProcurementAsync(
                pnl.ProcurementId
            );

            // Vendor → Items (per ProcOfferId) → Prices
            var vendors = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => new VendorItemOffersDto
                {
                    VendorId = group.Key,
                    Letters = group
                        .Where(x => !string.IsNullOrWhiteSpace(x.NoLetter))
                        .GroupBy(x => x.Round)
                        .OrderBy(g => g.Key)
                        .Select(g => g.Last().NoLetter ?? string.Empty)
                        .ToList(),
                    LetterDocIds = roundLetters
                        .Where(r => r.VendorId == group.Key)
                        .OrderBy(r => r.Round)
                        .Select(r => (string?)r.ProcDocumentId)
                        .ToList(),
                    Items = group
                        .GroupBy(offer => offer.ProcOfferId)
                        .Select(gg =>
                        {
                            var ordered = gg.OrderBy(x => x.Round).ToList();
                            var last = ordered.Last();

                            return new VendorOfferPerItemDto
                            {
                                VendorId = group.Key,
                                ProcOfferId = gg.Key,
                                Prices = ordered.Select(x => x.Price).ToList(),
                                Quantity = last.Quantity,
                                Trip = last.Trip,
                            };
                        })
                        .ToList(),
                })
                .ToList();

            var storedSelections = await _pnlRepository.GetSelectedVendorsAsync(pnl.ProcurementId);
            var selectedVendorIds =
                storedSelections.Count > 0
                    ? storedSelections.Select(x => x.VendorId).Distinct().ToList()
                    : vendors.Select(v => v.VendorId).Distinct().ToList();

            var items = pnl
                .Items.Select(item => new ProfitLossItemInputDto
                {
                    ProcOfferId = item.ProcOfferId,
                    Quantity = item.Quantity,
                    TarifAwal = item.TarifAwal,
                    TarifAdd = item.TarifAdd,
                    KmPer25 = item.KmPer25,
                    OperatorCost = item.OperatorCost,
                })
                .ToList();

            return new ProfitLossEditDto
            {
                ProfitLossId = pnl.ProfitLossId,
                ProcurementId = pnl.ProcurementId,
                AccrualAmount = pnl.AccrualAmount,
                RealizationAmount = pnl.RealizationAmount,
                Distance = pnl.Distance,
                Items = items,
                SelectedVendorId = pnl.SelectedVendorId,
                SelectedVendorFinalOffer = pnl.SelectedVendorFinalOffer,
                Profit = pnl.Profit,
                ProfitPercent = pnl.ProfitPercent,
                RowVersion = null,
                SelectedVendorIds = selectedVendorIds,
                Vendors = vendors,
            };
        }

        public async Task<decimal> GetTotalRevenueThisMonthAsync()
        {
            return await _pnlRepository.GetTotalRevenueThisMonthAsync();
        }

        public async Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto)
        {
            // Validasi input dasar
            if (dto.SelectedVendorIds == null || dto.SelectedVendorIds.Count == 0)
                throw new InvalidOperationException("Minimal 1 vendor harus dipilih.");

            var (opTotal, revTotal, items) = ComputeItems(dto.Items);
            if (items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            // Build vendor offers
            var offers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId);

            // VALIDASI KETAT: Harus ada minimal 1 vendor dengan penawaran lengkap
            if (offers.Count == 0)
                throw new InvalidOperationException(
                    "Minimal 1 vendor harus memberikan penawaran lengkap dengan harga, quantity, dan trip yang valid."
                );

            // Validasi setiap offer harus punya data lengkap
            var invalidOffers = offers
                .Where(o =>
                    o.Price <= 0
                    || o.Quantity <= 0
                    || string.IsNullOrWhiteSpace(o.ProcOfferId)
                    || string.IsNullOrWhiteSpace(o.VendorId)
                )
                .ToList();

            if (invalidOffers.Any())
                throw new InvalidOperationException(
                    $"Terdapat {invalidOffers.Count} penawaran vendor yang tidak valid. "
                        + "Pastikan semua field (Harga, Quantity, Trip) terisi dengan benar."
                );

            // Pilih vendor terbaik - WAJIB ada hasil
            var procOfferIds = items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = PickBestVendor(offers, procOfferIds);

            // Validasi vendor terpilih tidak boleh null
            if (string.IsNullOrWhiteSpace(bestVendorId))
                throw new InvalidOperationException(
                    "Gagal menentukan vendor terbaik. Pastikan minimal 1 vendor memberikan penawaran lengkap untuk semua item."
                );

            var selectedLetter = GetSelectedVendorLetter(offers, bestVendorId) ?? string.Empty;

            var profit = revTotal - bestTotal;
            var profitPct = revTotal > 0 ? (profit / revTotal) * 100m : 0m;

            // Biarkan null jika user tidak mengisi; summary akan fallback ke totalRevenue/totalOperatorCost
            var pnl = new ProfitLoss
            {
                ProcurementId = dto.ProcurementId,
                SelectedVendorId = bestVendorId, // WAJIB terisi
                SelectedVendorFinalOffer = bestTotal,
                NoLetterSelectedVendor = selectedLetter,
                Profit = profit,
                ProfitPercent = profitPct,
                AccrualAmount = dto.AccrualAmount,
                Distance = dto.Distance,
                RealizationAmount = dto.RealizationAmount,
                Items = items,
            };

            StampItemsWithProfitLossId(items, pnl.ProfitLossId);

            StampOffersWithProfitLossId(offers, pnl.ProfitLossId);
            await _pnlRepository.StoreProfitLossAggregateAsync(pnl, dto.SelectedVendorIds, offers);
            await UpdateRoundLettersAsync(dto.ProcurementId, pnl.ProfitLossId, dto.Vendors);
            return pnl;
        }

        public async Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto)
        {
            // Validasi input dasar
            if (dto.SelectedVendorIds == null || dto.SelectedVendorIds.Count == 0)
                throw new InvalidOperationException("Minimal 1 vendor harus dipilih.");

            var pnl =
                await _pnlRepository.GetByIdAsync(dto.ProfitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            var newOffers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId);

            // VALIDASI KETAT: Harus ada minimal 1 vendor dengan penawaran lengkap
            if (newOffers.Count == 0)
                throw new InvalidOperationException(
                    "Minimal 1 vendor harus memberikan penawaran lengkap dengan harga, quantity, dan trip yang valid."
                );

            // Validasi setiap offer harus punya data lengkap
            var invalidOffers = newOffers
                .Where(o =>
                    o.Price <= 0
                    || o.Quantity <= 0
                    || string.IsNullOrWhiteSpace(o.ProcOfferId)
                    || string.IsNullOrWhiteSpace(o.VendorId)
                )
                .ToList();

            if (invalidOffers.Any())
                throw new InvalidOperationException(
                    $"Terdapat {invalidOffers.Count} penawaran vendor yang tidak valid. "
                        + "Pastikan semua field (Harga, Quantity, Trip) terisi dengan benar."
                );

            var itemsByOffer = pnl.Items.ToDictionary(
                x => x.ProcOfferId,
                StringComparer.OrdinalIgnoreCase
            );

            decimal opTotal = 0m;
            decimal revTotal = 0m;

            foreach (var it in dto.Items)
            {
                if (!itemsByOffer.TryGetValue(it.ProcOfferId, out var entity))
                {
                    entity = new ProfitLossItem
                    {
                        ProfitLossId = pnl.ProfitLossId,
                        ProcOfferId = it.ProcOfferId,
                    };
                    pnl.Items.Add(entity);
                    itemsByOffer[it.ProcOfferId] = entity;
                }

                var operatorCost = it.TarifAdd * it.KmPer25;
                var revenue = (it.TarifAwal + operatorCost) * it.Quantity;

                entity.Quantity = it.Quantity;
                entity.TarifAwal = it.TarifAwal;
                entity.TarifAdd = it.TarifAdd;
                entity.KmPer25 = it.KmPer25;
                entity.OperatorCost = operatorCost;
                entity.Revenue = revenue;

                opTotal += operatorCost;
                revTotal += revenue;
            }

            if (pnl.Items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            // Pilih vendor terbaik - WAJIB ada hasil
            var procOfferIds = dto.Items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = PickBestVendor(newOffers, procOfferIds);

            // Validasi vendor terpilih tidak boleh null
            if (string.IsNullOrWhiteSpace(bestVendorId))
                throw new InvalidOperationException(
                    "Gagal menentukan vendor terbaik. Pastikan minimal 1 vendor memberikan penawaran lengkap untuk semua item."
                );

            var selectedLetter = GetSelectedVendorLetter(newOffers, bestVendorId) ?? string.Empty;
            var accrualAmount = dto.AccrualAmount ?? 0m;
            var realizationAmount = dto.RealizationAmount ?? 0m;

            pnl.SelectedVendorId = bestVendorId; // WAJIB terisi
            pnl.SelectedVendorFinalOffer = bestTotal;
            pnl.NoLetterSelectedVendor = selectedLetter;
            pnl.Profit = revTotal - bestTotal;
            pnl.ProfitPercent = revTotal > 0 ? (pnl.Profit / revTotal) * 100m : 0m;
            pnl.AccrualAmount = accrualAmount;
            pnl.RealizationAmount = realizationAmount;
            pnl.Distance = dto.Distance;
            pnl.UpdatedAt = DateTime.Now;

            StampOffersWithProfitLossId(newOffers, pnl.ProfitLossId);
            await _pnlRepository.UpdateProfitLossAggregateAsync(
                pnl,
                dto.SelectedVendorIds,
                newOffers
            );
            await UpdateRoundLettersAsync(dto.ProcurementId, pnl.ProfitLossId, dto.Vendors);
            return pnl;
        }

        public async Task<ProfitLoss?> GetLatestByProcurementAsync(string procurementId)
        {
            if (string.IsNullOrWhiteSpace(procurementId))
                throw new ArgumentException(
                    "ProcurementId tidak boleh kosong",
                    nameof(procurementId)
                );

            return await _pnlRepository.GetLatestByProcurementIdAsync(procurementId);
        }

        private static List<VendorOffer> BuildVendorOffersMulti(
            List<VendorItemOffersDto> input,
            string procurementId
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
                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                        continue;

                    var quantity = item.Quantity;
                    var trip = item.Trip;

                    // Skip jika quantity tidak valid
                    if (quantity <= 0)
                        continue;

                    for (int i = 0; i < (item.Prices?.Count ?? 0); i++)
                    {
                        var price = item.Prices![i];

                        // Skip jika price tidak valid
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
                                Quantity = quantity,
                                Trip = trip,
                            }
                        );
                    }
                }
            }

            return offers;
        }

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

        private static (
            decimal operatorCostTotal,
            decimal revenueTotal,
            List<ProfitLossItem> item
        ) ComputeItems(List<ProfitLossItemInputDto> input)
        {
            var items = new List<ProfitLossItem>();
            decimal opTotal = 0m,
                revTotal = 0m;

            foreach (var it in input)
            {
                var operatorCost = it.TarifAdd * it.KmPer25;
                var revenue = (it.TarifAwal + operatorCost) * it.Quantity;

                items.Add(
                    new ProfitLossItem
                    {
                        ProcOfferId = it.ProcOfferId,
                        Quantity = it.Quantity,
                        TarifAwal = it.TarifAwal,
                        TarifAdd = it.TarifAdd,
                        KmPer25 = it.KmPer25,
                        OperatorCost = operatorCost,
                        Revenue = revenue,
                    }
                );

                opTotal += operatorCost;
                revTotal += revenue;
            }

            return (opTotal, revTotal, items);
        }

        private static (
            string vendorId,
            decimal totalFinal,
            Dictionary<string, decimal> finalPerItem
        ) PickBestVendor(List<VendorOffer> offers, IEnumerable<string> procOfferIds)
        {
            if (offers == null || offers.Count == 0)
                throw new InvalidOperationException(
                    "Belum ada penawaran vendor yang bisa dihitung."
                );

            var requiredIds = procOfferIds?.Distinct().ToList() ?? new List<string>();

            // 1) Coba mode "lengkap semua item" (logika lama)
            var perVendorFull = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => new
                {
                    VendorId = group.Key,
                    FinalPerItem = group
                        .GroupBy(x => x.ProcOfferId)
                        .ToDictionary(
                            gg => gg.Key,
                            gg =>
                            {
                                var ordered = gg.OrderBy(x => x.Round).ToList();
                                var last = ordered.Last();
                                var minPrice = ordered.Min(x => x.Price);
                                return ComputeVendorItemCost(minPrice, last.Quantity, last.Trip);
                            }
                        ),
                })
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
            {
                // Masih ada vendor yang lengkap → pakai ini dulu
                return (perVendorFull.VendorId, perVendorFull.Total, perVendorFull.FinalPerItem);
            }

            // 2) Fallback: tidak ada vendor yang lengkap semua item
            //    Pilih vendor dengan total termurah dari item yang dia isi saja

            var perVendorLoose =
                offers
                    .GroupBy(o => o.VendorId)
                    .Select(group => new
                    {
                        VendorId = group.Key,
                        FinalPerItem = group
                            .GroupBy(x => x.ProcOfferId)
                            .ToDictionary(
                                gg => gg.Key,
                                gg =>
                                {
                                    var ordered = gg.OrderBy(x => x.Round).ToList();
                                    var last = ordered.Last();
                                    var minPrice = ordered.Min(x => x.Price);
                                    return ComputeVendorItemCost(
                                        minPrice,
                                        last.Quantity,
                                        last.Trip
                                    );
                                }
                            ),
                    })
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

        private static decimal ComputeRequiredTotal(
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

        private static decimal SafeSum(IEnumerable<decimal> values)
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

        private static decimal ComputeVendorItemCost(decimal price, decimal quantity, decimal trip)
        {
            var q = quantity <= 0 ? 1 : quantity;
            var t = trip <= 0 ? 1 : trip;

            return price * q * t;
        }

        private static decimal SafeMultiply(params decimal[] values)
        {
            decimal result = 1m;
            foreach (var value in values)
            {
                if (result == decimal.MaxValue || value == decimal.MaxValue)
                    return decimal.MaxValue;
                if (result == decimal.MinValue || value == decimal.MinValue)
                    return decimal.MinValue;

                try
                {
                    result *= value;
                }
                catch (OverflowException)
                {
                    return value < 0 ? decimal.MinValue : decimal.MaxValue;
                }
            }

            return result;
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
                    var letter = letters[i];
                    var round = i + 1;
                    await _roundLetterRepository.UpdateProfitLossLinkAsync(
                        procurementId,
                        vendor.VendorId,
                        round,
                        profitLossId,
                        letter
                    );
                }
            }

            await _roundLetterRepository.SaveChangesAsync();
        }

        // Coverage check dihapus agar penawaran parsial tetap diproses; PickBestVendor sudah meng-handle fallback.
    }
}
