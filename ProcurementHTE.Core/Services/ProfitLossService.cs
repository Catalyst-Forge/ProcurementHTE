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

        public Task<ProfitLoss?> GetByWorkOrderAsync(string woId)
        {
            return _pnlRepository.GetByWorkOrderAsync(woId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId)
        {
            return _pnlRepository.GetSelectedVendorsAsync(woId);
        }

        public async Task<ProfitLossSummaryDto> GetSummaryByWorkOrderAsync(string woId)
        {
            var pnl = await _pnlRepository.GetByWorkOrderAsync(woId);
            if (pnl == null)
                return null!;

            var allVendors = await _vendorRepository.GetAllAsync();
            var selectedRows = await _pnlRepository.GetSelectedVendorsAsync(woId);
            var offers = await _voRepository.GetByWorkOrderAsync(woId);

            var selectedVendorNames = selectedRows
                .Select(row =>
                    allVendors.FirstOrDefault(vendor => vendor.VendorId == row.VendorId)?.VendorName
                    ?? row.VendorId
                )
                .ToList();

            var vendorComparisons = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group =>
                {
                    var finalOffer = group.OrderBy(item => item.Round).Last().Price;
                    var profit = pnl.Revenue - finalOffer;
                    var profitPercent = pnl.Revenue > 0 ? (profit / pnl.Revenue) * 100m : 0m;
                    var vendorName =
                        allVendors
                            .FirstOrDefault(vendor => vendor.VendorId == group.Key)
                            ?.VendorName ?? group.Key;
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

            return new ProfitLossSummaryDto
            {
                ProfitLossId = pnl.ProfitLossId,
                WorkOrderId = pnl.WorkOrderId,
                TarifAwal = pnl.TarifAwal,
                TarifAdd = pnl.TarifAdd,
                KmPer25 = pnl.KmPer25,
                OperatorCost = pnl.OperatorCost,
                Revenue = pnl.Revenue,
                SelectedVendorId = pnl.SelectedVendorId ?? "",
                SelectedVendorName = vendorComparisons
                    .FirstOrDefault(vendor => vendor.IsSelected)
                    ?.VendorName,
                SelectedFinalOffer = pnl.SelectedVendorFinalOffer,
                Profit = pnl.Profit,
                ProfitPercent = pnl.ProfitPercent,
                SelectedVendorNames = selectedVendorNames,
                VendorComparisons = vendorComparisons,
            };
        }

        public async Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId)
        {
            var pnl =
                await _pnlRepository.GetByIdAsync(profitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            var offers = await _voRepository.GetByWorkOrderAsync(pnl.WorkOrderId);
            var vendors = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => new VendorOffersDto
                {
                    VendorId = group.Key,
                    Prices = group.OrderBy(item => item.Round).Select(item => item.Price).ToList(),
                })
                .ToList();

            var selectedVendorIds = vendors.Select(vendor => vendor.VendorId).Distinct().ToList();

            return new ProfitLossEditDto
            {
                ProfitLossId = pnl.ProfitLossId,
                WorkOrderId = pnl.WorkOrderId,
                TarifAwal = pnl.TarifAwal,
                TarifAdd = pnl.TarifAdd,
                KmPer25 = pnl.KmPer25,
                OperatorCost = pnl.OperatorCost,
                Revenue = pnl.Revenue,
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
            await _pnlRepository.RemoveSelectedVendorsAsync(dto.WorkOrderId);
            await _pnlRepository.StoreSelectedVendorsAsync(dto.WorkOrderId, dto.SelectedVendorIds);

            var offers = BuildVendorOffers(dto);
            if (offers.Count > 0)
                await _voRepository.StoreAllOffersAsync(offers);

            var profitLoss = CalculateProfitLoss(dto, offers);

            await _pnlRepository.StoreProfitLossAsync(profitLoss);

            return profitLoss;
        }

        public async Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto)
        {
            if (dto.SelectedVendorIds?.Count > 0)
            {
                await _pnlRepository.RemoveSelectedVendorsAsync(dto.WorkOrderId);
                await _pnlRepository.StoreSelectedVendorsAsync(
                    dto.WorkOrderId,
                    dto.SelectedVendorIds
                );
            }

            var pnl =
                await _pnlRepository.GetByIdAsync(dto.ProfitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            await _voRepository.RemoveByWorkOrderAsync(dto.WorkOrderId);

            var newOffers = BuildVendorOffersForUpdate(dto);
            if (newOffers.Count > 0)
                await _voRepository.StoreAllOffersAsync(newOffers);

            UpdateProfitLoss(pnl, dto, newOffers);
            await _pnlRepository.UpdateProfitLossAsync(pnl);
            return pnl;
        }

        public async Task<ProfitLoss?> GetLatestByWorkOrderAsync(string workOrderId)
        {
            if (string.IsNullOrWhiteSpace(workOrderId))
                throw new ArgumentException("WorkOrderId tidak boleh kosong", nameof(workOrderId));

            return await _pnlRepository.GetLatestByWorkOrderIdAsync(workOrderId);
        }

        private List<VendorOffer> BuildVendorOffers(ProfitLossInputDto dto)
        {
            var offers = new List<VendorOffer>();
            foreach (var vendor in dto.Vendors)
            {
                for (int i = 0; i < vendor.Prices.Count; i++)
                {
                    offers.Add(
                        new VendorOffer
                        {
                            WorkOrderId = dto.WorkOrderId,
                            VendorId = vendor.VendorId,
                            Round = i + 1,
                            Price = vendor.Prices[i],
                        }
                    );
                }
            }

            return offers;
        }

        private List<VendorOffer> BuildVendorOffersForUpdate(ProfitLossUpdateDto dto)
        {
            var offers = new List<VendorOffer>();
            foreach (var vendor in dto.Vendors)
            {
                for (int i = 0; i < vendor.Prices.Count; i++)
                {
                    offers.Add(
                        new VendorOffer
                        {
                            WorkOrderId = dto.WorkOrderId,
                            VendorId = vendor.VendorId,
                            Round = i + 1,
                            Price = vendor.Prices[i],
                        }
                    );
                }
            }

            return offers;
        }

        private ProfitLoss CalculateProfitLoss(ProfitLossInputDto dto, List<VendorOffer> offers)
        {
            var operatorCost = dto.TarifAdd * dto.KmPer25;
            var revenue = dto.TarifAwal + operatorCost;

            var finals = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => group.OrderBy(offer => offer.Round).Last())
                .ToList();

            if (!finals.Any())
                throw new InvalidOperationException("Tidak ada penawaran vendor");

            var best = finals.OrderBy(final => final.Price).First();
            var profit = revenue - best.Price;
            var profitPercent = revenue > 0 ? (profit / revenue) * 100m : 0m;

            return new ProfitLoss
            {
                WorkOrderId = dto.WorkOrderId,
                TarifAwal = dto.TarifAwal,
                TarifAdd = dto.TarifAdd,
                KmPer25 = dto.KmPer25,
                OperatorCost = operatorCost,
                Revenue = revenue,
                SelectedVendorId = best.VendorId,
                SelectedVendorFinalOffer = best.Price,
                Profit = profit,
                ProfitPercent = profitPercent,
            };
        }

        private void UpdateProfitLoss(
            ProfitLoss pnl,
            ProfitLossUpdateDto dto,
            List<VendorOffer> offers
        )
        {
            var operatorCost = dto.TarifAdd * dto.KmPer25;
            var revenue = dto.TarifAwal + operatorCost;

            var finals = offers
                .GroupBy(offer => offer.VendorId)
                .Select(group => group.OrderBy(item => item.Round).Last())
                .ToList();

            if (!finals.Any())
                throw new InvalidOperationException("Tidak ada penawaran vendor untuk dihitung");

            var best = finals.OrderBy(final => final.Price).First();

            pnl.TarifAwal = dto.TarifAwal;
            pnl.TarifAdd = dto.TarifAdd;
            pnl.KmPer25 = dto.KmPer25;
            pnl.OperatorCost = operatorCost;
            pnl.Revenue = revenue;
            pnl.SelectedVendorId = best.VendorId;
            pnl.SelectedVendorFinalOffer = best.Price;
            pnl.Profit = revenue - best.Price;
            pnl.ProfitPercent = revenue > 0 ? (pnl.Profit / revenue) * 100m : 0m;
            pnl.UpdatedAt = DateTime.Now;
        }
    }
}
