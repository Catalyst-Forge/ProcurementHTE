using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        public async Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto)
        {
            if (dto.SelectedVendorIds == null || dto.SelectedVendorIds.Count == 0)
                throw new InvalidOperationException("Minimal 1 vendor harus dipilih.");

            var pnl =
                await _pnlRepository.GetByIdAsync(dto.ProfitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

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

            var newOffers = BuildVendorOffersMulti(dto.Vendors, dto.ProcurementId, jobTypeName);
            ValidateVendorOffers(newOffers);
            var (_, revTotal) = ApplyUpdatedItems(pnl, dto, jobTypeName);

            if (pnl.Items.Count == 0)
                throw new InvalidOperationException("Minimal 1 item PnL diperlukan.");

            var procOfferIds = dto.Items.Select(item => item.ProcOfferId).ToList();
            var (bestVendorId, bestTotal, _) = ProfitLossCalculator.PickBestVendor(
                newOffers,
                procOfferIds
            );
            if (string.IsNullOrWhiteSpace(bestVendorId))
                throw new InvalidOperationException(
                    "Gagal menentukan vendor terbaik. Pastikan minimal 1 vendor memberikan penawaran lengkap untuk semua item."
                );

            var selectedLetter = GetSelectedVendorLetter(newOffers, bestVendorId) ?? string.Empty;
            pnl.SelectedVendorId = bestVendorId;
            pnl.SelectedVendorFinalOffer = bestTotal;
            pnl.NoLetterSelectedVendor = selectedLetter;
            pnl.Profit = revTotal - bestTotal;
            pnl.ProfitPercent = revTotal > 0 ? (pnl.Profit / revTotal) * 100m : 0m;
            pnl.AccrualAmount = dto.AccrualAmount ?? 0m;
            pnl.RealizationAmount = dto.RealizationAmount ?? 0m;
            pnl.Distance = dto.Distance;
            pnl.TglMulaiSewa = dto.TglMulaiSewa;
            pnl.TglMulaiMoving = dto.TglMulaiMoving;
            pnl.UpdatedAt = DateTime.Now;

            StampOffersWithProfitLossId(newOffers, pnl.ProfitLossId);

            await UpdateProcOfferUnitRevenueAsync(dto.Items);
            await _pnlRepository.UpdateProfitLossAggregateAsync(
                pnl,
                dto.SelectedVendorIds,
                newOffers
            );
            await UpdateRoundLettersAsync(dto.ProcurementId, pnl.ProfitLossId, dto.Vendors);

            return pnl;
        }
    }
}
