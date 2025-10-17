using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class ProfitLossService : IProfitLossService
    {
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IVendorOfferRepository _voRepository;
        private readonly IWorkOrderRepository _woRepository;

        public ProfitLossService(
            IProfitLossRepository pnlRepository,
            IVendorOfferRepository voRepository,
            IWorkOrderRepository woRepository
        )
        {
            _pnlRepository = pnlRepository;
            _voRepository = voRepository;
            _woRepository = woRepository;
        }

        public async Task<IEnumerable<ProfitLoss>> GetAllProfitLossAsync()
        {
            return await _pnlRepository.GetAllAsync();
        }

        public async Task<ProfitLoss?> GetProfitLossByIdAsync(string id) {
            return await _pnlRepository.GetByIdAsync(id);
        }

        public async Task<ProfitLoss?> GetProfitLossWithWorkOrderAsync(string woId)
        {
            return await _pnlRepository.GetByWorkOrderAsync(woId);
        }

        public async Task<ProfitLoss> CreateProfitLossWithOffersAsync(
            IEnumerable<VendorOfferInputDto> vendorOffers,
            CreateProfitLossInputDto pnlInput
        )
        {
            if (string.IsNullOrWhiteSpace(pnlInput.WorkOrderId))
                throw new ArgumentException("Work Order wajib diisi");

            var validOffers = vendorOffers
                .Where(vo => !string.IsNullOrWhiteSpace(vo.VendorId) && (vo.OfferPrice ?? 0) > 0)
                .ToList();
            if (!validOffers.Any())
                throw new ArgumentException("Minimal satu penawaran vendor valid");

            try
            {
                // Save VendorOffers
                var offers = validOffers
                    .Select(offer => new VendorOffer
                    {
                        WorkOrderId = pnlInput.WorkOrderId,
                        VendorId = offer.VendorId,
                        ItemName = offer.ItemName,
                        Trip = offer.Trip,
                        Unit = offer.Unit,
                        OfferPrice = offer.OfferPrice,
                        OfferNumber = offer.OfferNumber,
                        OfferDate = DateTime.Now,
                    })
                    .ToList();

                await _voRepository.StoreVendorOfferAsync(offers);

                // Menentukan best offer
                VendorOffer bestOffer = offers
                    .OrderBy(offer => offer.OfferPrice ?? decimal.MaxValue)
                    .First();

                if (!string.IsNullOrWhiteSpace(pnlInput.SelectedVendorId))
                {
                    var offer = offers.FirstOrDefault(offer =>
                        offer.VendorId == pnlInput.SelectedVendorId
                    );
                    if (offer != null)
                        bestOffer = offer;
                }

                decimal revenue = pnlInput.Revenue;
                decimal bestOfferPrice = bestOffer.OfferPrice ?? 0m;
                decimal costOperator = pnlInput.CostOperator;

                // Hitung Profit & Loss
                decimal profit = revenue - bestOfferPrice - costOperator;
                decimal profitPercentage =
                    revenue > 0 ? Math.Round((profit / revenue) * 100m, 2) : 0m;
                decimal adjustmentRate = pnlInput.AdjustmentRate / 100m;
                decimal adjustedProfit = Math.Round(profit * (1 + adjustmentRate), 2);

                // Membuat Profit & Loss
                var pnl = new ProfitLoss
                {
                    WorkOrderId = pnlInput.WorkOrderId,
                    Revenue = revenue,
                    CostOperator = costOperator,
                    Profit = profit,
                    ProfitPercentage = profitPercentage,
                    BestOfferPrice = bestOfferPrice,
                    HargaPenawaran1 = offers.ElementAtOrDefault(0)?.OfferPrice,
                    HargaPenawaran2 = offers.ElementAtOrDefault(1)?.OfferPrice,
                    HargaPenawaran3 = offers.ElementAtOrDefault(2)?.OfferPrice,
                    AdjustmentRate = pnlInput.AdjustmentRate,
                    AdjustedProfit = adjustedProfit,
                    PotentialNewProfit = adjustedProfit,
                    ProfitRevenue = revenue > 0 ? Math.Round(profit / revenue, 4) : 0,
                    SelectedVendorOfferId = bestOffer.VendorOfferId,
                };

                await _pnlRepository.StoreProfitLossAsync(pnl);
                return pnl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal membuat Profit and Loss: {ex.Message}", ex);
            }
        }

        public async Task<ProfitLoss> UpdateProfitLossAsync(string id, UpdateProfitLossDto dto)
        {
            var pnl = await _pnlRepository.GetByIdAsync(id);
            if (pnl == null)
                throw new InvalidOperationException("Profit and Loss tidak ditemukan");

            pnl.CostOperator = dto.CostOperator;
            pnl.AdjustmentRate = dto.AdjustmentRate;
            pnl.UpdatedAt = DateTime.Now;

            // Recalculate
            pnl.Profit = pnl.Revenue - pnl.CostOperator;
            pnl.ProfitPercentage = pnl.Revenue > 0 ? (pnl.Profit / pnl.Revenue) * 100 : 0;
            pnl.AdjustedProfit = pnl.Profit * (1 - pnl.AdjustmentRate);
            pnl.PotentialNewProfit = pnl.Profit - (pnl.BestOfferPrice - pnl.CostOperator);

            await _pnlRepository.UpdateProfitLossAsync(pnl);
            return pnl;
        }

        public async Task<ProfitLoss> CalculateProfitLossAsync(
            string id,
            string selectedVendorOfferId
        )
        {
            var wo = await _woRepository.GetWithOffersAsync(id);
            if (wo != null)
            {
                var pnl = await _pnlRepository.GetByWorkOrderAsync(id);
                pnl ??= new ProfitLoss
                {
                    WorkOrderId = id,
                    SelectedVendorOfferId = selectedVendorOfferId,
                    CreatedAt = wo.CreatedAt,
                    AdjustmentRate = 0.15m,
                };

                return await CalculateAndSaveAsync(pnl, wo);
            }

            throw new InvalidOperationException("Work order tidak ditemukan");
        }

        public async Task DeleteProfitLossAsync(string id)
        {
            ArgumentNullException.ThrowIfNull(id);
            await _pnlRepository.DropProfitLossAsync(id);
        }

        private async Task<ProfitLoss> CalculateAndSaveAsync(ProfitLoss profitLoss, WorkOrder wo)
        {
            var offers = wo.VendorOffers.OrderBy(x => x.OfferPrice).ToList();

            if (offers.Count > 0)
                profitLoss.HargaPenawaran1 = offers[0].OfferPrice;
            if (offers.Count > 1)
                profitLoss.HargaPenawaran2 = offers[1].OfferPrice;
            if (offers.Count > 2)
                profitLoss.HargaPenawaran3 = offers[2].OfferPrice;

            profitLoss.BestOfferPrice = offers.Min(x => x.OfferPrice);
            profitLoss.Profit = profitLoss.Revenue - profitLoss.CostOperator;
            profitLoss.ProfitPercentage =
                profitLoss.Revenue > 0 ? (profitLoss.Profit / profitLoss.Revenue) * 100 : 0;
            profitLoss.AdjustedProfit = profitLoss.Profit * (1 - profitLoss.AdjustmentRate);
            profitLoss.PotentialNewProfit =
                profitLoss.Profit - (profitLoss.BestOfferPrice - profitLoss.CostOperator);

            await _pnlRepository.UpdateProfitLossAsync(profitLoss);
            return profitLoss;
        }
    }
}
