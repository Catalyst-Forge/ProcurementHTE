using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        public async Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto)
        {
            if (dto.SelectedVendorIds == null || dto.SelectedVendorIds.Count == 0)
                throw new InvalidOperationException("Minimal 1 vendor harus dipilih.");

            var procurement = await _pnlRepository.GetProcurementWithJobTypeAsync(
                dto.ProcurementId
            );
            if (procurement == null)
                throw new KeyNotFoundException($"Procurement {dto.ProcurementId} tidak ditemukan.");

            var jobTypeName = procurement.JobType?.TypeName
                ?? throw new InvalidOperationException(
                    "JobType tidak ditemukan untuk Procurement ini."
                );

            _jobTypeCalc.ValidateRequiredFields(
                new ProfitLoss
                {
                    Distance = dto.Distance,
                    ProcurementId = dto.ProcurementId,
                    TglMulaiSewa = dto.TglMulaiSewa,
                    TglMulaiMoving = dto.TglMulaiMoving,
                },
                jobTypeName
            );

            var (_, revTotal, items) = ComputeItems(
                dto.Items,
                jobTypeName,
                dto.Distance
            );
            if (items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            var offers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId, jobTypeName);
            ValidateVendorOffers(offers);

            var procOfferIds = items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = ProfitLossCalculator.PickBestVendor(
                offers,
                procOfferIds
            );
            if (string.IsNullOrWhiteSpace(bestVendorId))
                throw new InvalidOperationException(
                    "Gagal menentukan vendor terbaik. Pastikan minimal 1 vendor memberikan penawaran lengkap untuk semua item."
                );

            var selectedLetter = GetSelectedVendorLetter(offers, bestVendorId) ?? string.Empty;
            var profit = revTotal - bestTotal;

            var pnl = new ProfitLoss
            {
                ProcurementId = dto.ProcurementId,
                SelectedVendorId = bestVendorId,
                SelectedVendorFinalOffer = bestTotal,
                NoLetterSelectedVendor = selectedLetter,
                Profit = profit,
                ProfitPercent = revTotal > 0 ? (profit / revTotal) * 100m : 0m,
                AccrualAmount = dto.AccrualAmount,
                Distance = dto.Distance,
                RealizationAmount = dto.RealizationAmount,
                TglMulaiSewa = dto.TglMulaiSewa,
                TglMulaiMoving = dto.TglMulaiMoving,
                Items = items,
            };

            StampItemsWithProfitLossId(items, pnl.ProfitLossId);
            StampOffersWithProfitLossId(offers, pnl.ProfitLossId);

            await UpdateProcOfferUnitRevenueAsync(dto.Items);
            await _pnlRepository.StoreProfitLossAggregateAsync(pnl, dto.SelectedVendorIds, offers);
            await UpdateRoundLettersAsync(dto.ProcurementId, pnl.ProfitLossId, dto.Vendors);

            return pnl;
        }
    }
}
