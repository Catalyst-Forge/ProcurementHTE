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
        private readonly IJobTypeCalculationService _jobTypeCalc;

        public ProfitLossService(
            IProfitLossRepository pnlRepository,
            IVendorOfferRepository voRepository,
            IVendorRepository vendorRepository,
            IVendorRoundLetterRepository roundLetterRepository,
            IJobTypeCalculationService jobTypeCalculationService
        )
        {
            _pnlRepository = pnlRepository;
            _voRepository = voRepository;
            _vendorRepository = vendorRepository;
            _roundLetterRepository = roundLetterRepository;
            _jobTypeCalc = jobTypeCalculationService;
        }

        public async Task<bool> DeleteByProcurementAsync(string procurementId, string deletedByUserId)
        {
            if (string.IsNullOrWhiteSpace(procurementId))
                return false;
                
            var pnl = await _pnlRepository.GetByProcurementAsync(procurementId);
            if (pnl == null)
                return false;
                
            await _pnlRepository.DeleteAsync(pnl.ProfitLossId, deletedByUserId);
            return true;
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
            var totalOperatorCost = SafeSum(pnl.Items.Select(item => item.OperatorCost ?? 0));

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
                                return ComputeVendorItemCost(minPrice, last.QuantityItem, last.QuantityOfUnit);
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
                    var procOffer = pnl.Items.Select(i => i.ProcOffer)
                            .FirstOrDefault(i => i.ProcOfferId == item.ProcOfferId);

                    var itemName = procOffer?.ItemPenawaran ?? item.ProcOfferId;

                    var unitRevenue = procOffer?.UnitRevenue
                        ?? (string?)(item.UnitType?.Name ?? item.UnitTypeId);
                    var unitItems = procOffer?.Unit;
                    var quantity = item.Quantity.HasValue ? (int?)Convert.ToInt32(item.Quantity.Value) : null;

                    return (
                        item.ProcOfferId,
                        itemName,
                        item.UnitQty,
                        item.BasePrice,
                        item.TarifAdd,
                        item.KmPer25,
                        item.OperatorCost,
                        item.Revenue,
                        quantity,
                        unitRevenue,
                        unitItems
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
                Distance = pnl.Distance ?? 0,
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
                                Quantity = last.QuantityItem,
                                Trip = last.QuantityOfUnit,
                                // If item exists in database, it means it was included when saved
                                IsIncluded = true,
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

            // For Moving/StandBy: UnitQty = QtyItems, Quantity = Quantity/Durasi
            // For Angkutan: UnitQty = Quantity (trips), Quantity = null
            var items = pnl
                .Items.Select(item => new ProfitLossItemInputDto
                {
                    ProcOfferId = item.ProcOfferId,
                    // For edit, Quantity field represents Quantity/Durasi for SEWA_UNIT/MOVING
                    // and UnitQty (trips) for PENGANGKUTAN
                    Quantity = item.Quantity.HasValue ? (int)item.Quantity.Value : item.UnitQty,
                    QtyItems = item.UnitQty, // For Moving/StandBy, this is the unit count
                    TarifAwal = item.BasePrice,
                    TarifAdd = item.TarifAdd ?? 0,
                    KmPer25 = item.KmPer25 ?? 0,
                    OperatorCost = item.OperatorCost ?? 0,
                })
                .ToList();

            return new ProfitLossEditDto
            {
                ProfitLossId = pnl.ProfitLossId,
                ProcurementId = pnl.ProcurementId,
                AccrualAmount = pnl.AccrualAmount,
                RealizationAmount = pnl.RealizationAmount,
                Distance = pnl.Distance ?? 0,
                TglMulaiSewa = pnl.TglMulaiSewa,
                TglMulaiMoving = pnl.TglMulaiMoving,
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

            // Get Procurement with JobType for calculation
            var procurement = await _pnlRepository.GetProcurementWithJobTypeAsync(dto.ProcurementId);
            if (procurement == null)
                throw new KeyNotFoundException($"Procurement {dto.ProcurementId} tidak ditemukan.");

            var jobTypeName = procurement.JobType?.TypeName
                ?? throw new InvalidOperationException("JobType tidak ditemukan untuk Procurement ini.");

            // Validate required fields berdasarkan JobType
            var tempPnl = new ProfitLoss
            {
                Distance = dto.Distance,
                ProcurementId = dto.ProcurementId,
                TglMulaiSewa = dto.TglMulaiSewa,
                TglMulaiMoving = dto.TglMulaiMoving,
            };
            _jobTypeCalc.ValidateRequiredFields(tempPnl, jobTypeName);

            var (opTotal, revTotal, items) = ComputeItems(dto.Items, jobTypeName, dto.Distance);
            if (items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            // Build vendor offers
            var offers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId, jobTypeName);

            // VALIDASI KETAT: Harus ada minimal 1 vendor dengan penawaran lengkap
            if (offers.Count == 0)
                throw new InvalidOperationException(
                    "Minimal 1 vendor harus memberikan penawaran lengkap dengan harga, quantity, dan trip yang valid."
                );

            // Validasi setiap offer harus punya data lengkap
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
                TglMulaiSewa = dto.TglMulaiSewa,
                TglMulaiMoving = dto.TglMulaiMoving,
                Items = items,
            };

            StampItemsWithProfitLossId(items, pnl.ProfitLossId);

            StampOffersWithProfitLossId(offers, pnl.ProfitLossId);

            // Update ProcOffer.UnitRevenue from items
            await UpdateProcOfferUnitRevenueAsync(dto.Items);

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

            // Get Procurement with JobType for calculation
            var procurement = await _pnlRepository.GetProcurementWithJobTypeAsync(dto.ProcurementId);
            if (procurement == null)
                throw new KeyNotFoundException($"Procurement {dto.ProcurementId} tidak ditemukan.");

            var jobTypeName = procurement.JobType?.TypeName
                ?? throw new InvalidOperationException("JobType tidak ditemukan untuk Procurement ini.");

            // Validate required fields berdasarkan JobType
            var tempPnl = new ProfitLoss
            {
                Distance = dto.Distance,
                ProcurementId = dto.ProcurementId,
                TglMulaiSewa = dto.TglMulaiSewa,
                TglMulaiMoving = dto.TglMulaiMoving,
            };
            _jobTypeCalc.ValidateRequiredFields(tempPnl, jobTypeName);

            var newOffers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId, jobTypeName);

            // VALIDASI KETAT: Harus ada minimal 1 vendor dengan penawaran lengkap
            if (newOffers.Count == 0)
                throw new InvalidOperationException(
                    "Minimal 1 vendor harus memberikan penawaran lengkap dengan harga, quantity, dan trip yang valid."
                );

            // Validasi setiap offer harus punya data lengkap
            var invalidOffers = newOffers
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
                        ItemName = "",
                    };
                    pnl.Items.Add(entity);
                    itemsByOffer[it.ProcOfferId] = entity;
                }

                // Determine if this JobType uses Quantity for calculation (SEWA_UNIT, MOVING)
                var usesQuantityForCalc = !_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName);

                // For SEWA_UNIT/MOVING:
                //   - UnitQty = QtyItems (jumlah unit fisik dari ProcOffer)
                //   - Quantity = Quantity/Durasi (durasi sewa)
                // For PENGANGKUTAN:
                //   - UnitQty = Quantity (number of trips)
                //   - Quantity = null (not used)
                var unitQty = usesQuantityForCalc ? it.QtyItems : it.Quantity;
                var quantityDurasi = usesQuantityForCalc ? it.Quantity : (decimal?)null;

                // Create temp item for calculation
                var tempItem = new ProfitLossItem
                {
                    UnitQty = unitQty,
                    BasePrice = it.TarifAwal,
                    TarifAdd = it.TarifAdd,
                    KmPer25 = it.KmPer25,
                    Quantity = quantityDurasi,
                };

                var revenue = _jobTypeCalc.CalculateItemRevenue(tempItem, jobTypeName, dto.Distance);

                decimal operatorCost;
                if (_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName))
                {
                    // PENGANGKUTAN mode
                    var kmPer25 = _jobTypeCalc.CalculateKmPer25(dto.Distance ?? 0);
                    operatorCost = _jobTypeCalc.CalculateOperatorCost(it.TarifAdd, kmPer25);

                    entity.UnitQty = it.Quantity;
                    entity.BasePrice = it.TarifAwal;
                    entity.TarifAdd = it.TarifAdd;
                    entity.KmPer25 = kmPer25;
                    entity.OperatorCost = operatorCost;
                    entity.Quantity = null;
                }
                else
                {
                    // SEWA_UNIT or MOVING mode
                    operatorCost = 0m;
                    entity.UnitQty = it.QtyItems;  // QtyItems = jumlah unit fisik
                    entity.BasePrice = it.TarifAwal;
                    entity.TarifAdd = null;
                    entity.KmPer25 = null;
                    entity.OperatorCost = null;
                    entity.Quantity = it.Quantity; // Quantity = durasi sewa
                }

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
            pnl.TglMulaiSewa = dto.TglMulaiSewa;
            pnl.TglMulaiMoving = dto.TglMulaiMoving;
            pnl.UpdatedAt = DateTime.Now;

            StampOffersWithProfitLossId(newOffers, pnl.ProfitLossId);

            // Update ProcOffer.UnitRevenue from items
            await UpdateProcOfferUnitRevenueAsync(dto.Items);

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
                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                        continue;

                    var quantityItem = item.Quantity;
                    var quantityOfUnit = item.Trip;

                    // Skip jika quantity tidak valid
                    if (quantityItem <= 0)
                        continue;

                    // Determine UnitTypeId based on JobType
                    var unitTypeId = _jobTypeCalc.GetVendorOfferUnitTypeId(jobTypeName, null);

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
                                QuantityItem = quantityItem,
                                QuantityOfUnit = quantityOfUnit,
                                UnitTypeId = unitTypeId,
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

        private (
            decimal operatorCostTotal,
            decimal revenueTotal,
            List<ProfitLossItem> item
        ) ComputeItems(List<ProfitLossItemInputDto> input, string jobTypeName, decimal? distance)
        {
            var items = new List<ProfitLossItem>();
            decimal opTotal = 0m,
                revTotal = 0m;

            foreach (var it in input)
            {
                // Determine if this JobType uses Quantity for calculation (SEWA_UNIT, MOVING)
                var usesQuantityForCalc = !_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName);

                // For SEWA_UNIT/MOVING:
                //   - UnitQty = QtyItems (jumlah unit fisik dari ProcOffer)
                //   - Quantity = Quantity/Durasi (durasi sewa)
                // For PENGANGKUTAN:
                //   - UnitQty = Quantity (number of trips)
                //   - Quantity = null (not used)
                var unitQty = usesQuantityForCalc ? it.QtyItems : it.Quantity;
                var quantityDurasi = usesQuantityForCalc ? it.Quantity : (decimal?)null;

                // Create temporary item for calculation
                var tempItem = new ProfitLossItem
                {
                    ProcOfferId = it.ProcOfferId,
                    UnitQty = unitQty,
                    BasePrice = it.TarifAwal,
                    TarifAdd = it.TarifAdd,
                    KmPer25 = it.KmPer25,
                    // Set Quantity BEFORE CalculateItemRevenue for SEWA_UNIT/MOVING
                    Quantity = quantityDurasi,
                    ItemName = "", // Will be populated from ProcOffer
                };

                // Calculate using JobTypeCalculationService
                var revenue = _jobTypeCalc.CalculateItemRevenue(tempItem, jobTypeName, distance);

                // For PENGANGKUTAN, recalculate OperatorCost
                decimal operatorCost;
                if (_jobTypeCalc.IsDistanceRequiredForCalculation(jobTypeName))
                {
                    // PENGANGKUTAN mode
                    var kmPer25 = _jobTypeCalc.CalculateKmPer25(distance ?? 0);
                    operatorCost = _jobTypeCalc.CalculateOperatorCost(it.TarifAdd, kmPer25);

                    tempItem.KmPer25 = kmPer25;
                    tempItem.OperatorCost = operatorCost;
                    tempItem.Quantity = null; // Not used in PENGANGKUTAN
                }
                else
                {
                    // SEWA_UNIT or MOVING mode
                    operatorCost = 0m;
                    tempItem.TarifAdd = null;
                    tempItem.KmPer25 = null;
                    tempItem.OperatorCost = null;
                    // Quantity already set above
                }

                tempItem.Revenue = revenue;

                items.Add(tempItem);

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
                                return ComputeVendorItemCost(minPrice, last.QuantityItem, last.QuantityOfUnit);
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
                                        last.QuantityItem,
                                        last.QuantityOfUnit
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

        /// <summary>
        /// Updates ProcOffer.UnitRevenue from ProfitLoss items
        /// This ensures the UnitRevenue selection is persisted to the ProcOffer table
        /// </summary>
        private async Task UpdateProcOfferUnitRevenueAsync(List<ProfitLossItemInputDto> items)
        {
            if (items == null || items.Count == 0)
                return;

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ProcOfferId) || string.IsNullOrWhiteSpace(item.UnitRevenue))
                    continue;

                await _pnlRepository.UpdateProcOfferUnitRevenueAsync(item.ProcOfferId, item.UnitRevenue);
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
