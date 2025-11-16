using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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

        public ProfitLossService(
            IProfitLossRepository pnlRepository,
            IVendorOfferRepository voRepository,
            IVendorRepository vendorRepository
        )
        {
            _pnlRepository = pnlRepository;
            _voRepository = voRepository;
            _vendorRepository = vendorRepository;
        }

        public Task<ProfitLoss?> GetByProcurementAsync(string woId)
        {
            return _pnlRepository.GetByProcurementAsync(woId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId)
        {
            return _pnlRepository.GetSelectedVendorsAsync(woId);
        }

        public async Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(string woId)
        {
            var pnl = await _pnlRepository.GetByProcurementAsync(woId);
            if (pnl == null)
                return null!;

            var allVendors = await _vendorRepository.GetAllAsync();
            var selectedRows = await _pnlRepository.GetSelectedVendorsAsync(woId);
            var offers = await _voRepository.GetByProcurementAsync(woId);

            var totalRevenue = pnl.Items.Sum(item => item.Revenue);
            var totalOperatorCost = pnl.Items.Sum(item => item.OperatorCost);

            var selectedVendorNames = selectedRows
                .Select(row =>
                    allVendors.FirstOrDefault(vendor => vendor.VendorId == row.VendorId)?.VendorName
                    ?? row.VendorId
                )
                .ToList();

            var vendorTotals = offers
                .GroupBy(vendorOffer => vendorOffer.VendorId)
                .Select(group =>
                {
                    var finalPerItem = group
                        .GroupBy(vendorOffer => vendorOffer.ProcOfferId)
                        .ToDictionary(
                            groupVendorOffer => groupVendorOffer.Key,
                            groupVendorOffer =>
                                groupVendorOffer
                                    .OrderBy(vendorOffer => vendorOffer.Round)
                                    .Last()
                                    .Price
                        );

                    // Wajib quote semua item agar valid
                    var requiredItemIds = pnl.Items.Select(item => item.ProcOfferId).ToList();
                    var total = requiredItemIds.Sum(id =>
                        finalPerItem.TryGetValue(id, out var p) ? p : decimal.MaxValue
                    );

                    return new
                    {
                        group.Key,
                        finalPerItem,
                        total,
                    };
                })
                .Where(x => x.total < decimal.MaxValue)
                .ToList();

            var vendorComparisons = vendorTotals
                .Select(x =>
                {
                    var final = x.total;
                    var profit = totalRevenue - final;
                    var profitPercent = totalRevenue > 0 ? (profit / totalRevenue) * 100m : 0m;
                    var vendorName =
                        allVendors.FirstOrDefault(vendor => vendor.VendorId == x.Key)?.VendorName
                        ?? x.Key;
                    var isSelected = pnl.SelectedVendorId == x.Key;

                    return new VendorComparisonDto
                    {
                        VendorName = vendorName,
                        FinalOffer = final,
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
                        pnl.Procurement?.ProcOffers?.FirstOrDefault(procOffer =>
                            procOffer.ProcOfferId == item.ProcOfferId
                        )?.ItemPenawaran ?? item.ProcOfferId;

                    return (
                        item.ProcOfferId,
                        ItemName: itemName,
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
                SelectedVendorName = vendorComparisons
                    .FirstOrDefault(vendor => vendor.IsSelected)
                    ?.VendorName,
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

            // Vendor → Items (per ProcOfferId) → Prices
            var vendors = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => new VendorItemOffersDto
                {
                    VendorId = group.Key,
                    Items = group
                        .GroupBy(offer => offer.ProcOfferId)
                        .Select(gg => new VendorOfferPerItemDto
                        {
                            VendorId = group.Key,
                            ProcOfferId = gg.Key,
                            Prices = gg.OrderBy(x => x.Round).Select(x => x.Price).ToList(),
                            Letters = gg.OrderBy(offer => offer.Round)
                                .Select(vendorOffer => vendorOffer.NoLetter ?? string.Empty)
                                .ToList(),
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
            await _pnlRepository.RemoveSelectedVendorsAsync(dto.ProcurementId);
            await _pnlRepository.StoreSelectedVendorsAsync(
                dto.ProcurementId,
                dto.SelectedVendorIds
            );

            var offers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId);
            if (offers.Count > 0)
                await _voRepository.StoreAllOffersAsync(offers);

            // Hitung item & total
            var (opTotal, revTotal, items) = ComputeItems(dto.Items);
            if (items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            // Pilih vendor terbaik dari total semua item
            var procOfferIds = items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = PickBestVendor(offers, procOfferIds);

            var profit = revTotal - bestTotal;
            var profitPct = revTotal > 0 ? (profit / revTotal) * 100m : 0m;
            var accrualAmount = dto.AccrualAmount ?? 0m;
            var realizationAmount = dto.RealizationAmount ?? 0m;

            var pnl = new ProfitLoss
            {
                ProcurementId = dto.ProcurementId,
                SelectedVendorId = bestVendorId,
                SelectedVendorFinalOffer = bestTotal,
                Profit = profit,
                ProfitPercent = profitPct,
                AccrualAmount = accrualAmount,
                Distance = dto.Distance,
                RealizationAmount = realizationAmount,
                Items = items,
            };

            StampItemsWithProfitLossId(items, pnl.ProfitLossId);

            await _pnlRepository.StoreProfitLossAsync(pnl);
            return pnl;
        }

        public async Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto)
        {
            if (dto.SelectedVendorIds?.Count > 0)
            {
                await _pnlRepository.RemoveSelectedVendorsAsync(dto.ProcurementId);
                await _pnlRepository.StoreSelectedVendorsAsync(
                    dto.ProcurementId,
                    dto.SelectedVendorIds
                );
            }

            var pnl =
                await _pnlRepository.GetByIdAsync(dto.ProfitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            await _voRepository.RemoveByProcurementAsync(dto.ProcurementId);

            var newOffers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId);
            if (newOffers.Count > 0)
                await _voRepository.StoreAllOffersAsync(newOffers);

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

            var procOfferIds = dto.Items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = PickBestVendor(newOffers, procOfferIds);

            var accrualAmount = dto.AccrualAmount ?? 0m;
            var realizationAmount = dto.RealizationAmount ?? 0m;

            pnl.SelectedVendorId = bestVendorId;
            pnl.SelectedVendorFinalOffer = bestTotal;
            pnl.Profit = revTotal - bestTotal;
            pnl.ProfitPercent = revTotal > 0 ? (pnl.Profit / revTotal) * 100m : 0m;
            pnl.AccrualAmount = accrualAmount;
            pnl.RealizationAmount = realizationAmount;
            pnl.Distance = dto.Distance;
            pnl.UpdatedAt = DateTime.Now;

            await _pnlRepository.UpdateProfitLossAsync(pnl);
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
            string procurementId
        )
        {
            var offers = new List<VendorOffer>();

            foreach (var vendor in input)
            {
                if (string.IsNullOrWhiteSpace(vendor.VendorId))
                    continue;

                foreach (var item in vendor.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                        continue;

                    var letters = item.Letters ?? new List<string>();
                    for (int i = 0; i < (item.Prices?.Count ?? 0); i++)
                    {
                        offers.Add(
                            new VendorOffer
                            {
                                ProcurementId = procurementId,
                                VendorId = vendor.VendorId,
                                ProcOfferId = item.ProcOfferId,
                                Round = i + 1,
                                Price = item.Prices![i],
                                NoLetter = letters.Count > i ? letters[i] : null,
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

        private (
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
            // final per vendor = sum(final price per item)
            var perVendor = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => new
                {
                    VendorId = group.Key,
                    FinalPerItem = group
                        .GroupBy(x => x.ProcOfferId) // per item (ProcOffer)
                        .ToDictionary(
                            gg => gg.Key,
                            gg => gg.OrderBy(x => x.Round).Last().Price // final round per item
                        ),
                })
                .Select(x => new
                {
                    x.VendorId,
                    Total = procOfferIds.Sum(id =>
                        x.FinalPerItem.TryGetValue(id, out var p) ? p : decimal.MaxValue
                    ),
                    x.FinalPerItem,
                })
                .Where(x => x.Total < decimal.MaxValue)
                .OrderBy(x => x.Total)
                .FirstOrDefault();

            if (perVendor == null)
                throw new InvalidOperationException(
                    "Tidak ada penawaran vendor yang lengkap untuk semua item."
                );

            return (perVendor.VendorId, perVendor.Total, perVendor.FinalPerItem);
        }
    }
}
