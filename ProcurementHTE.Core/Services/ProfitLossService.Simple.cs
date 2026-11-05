using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        /// <summary>
        /// LTC and RO only record the winning vendor, the revenue and the vendor
        /// cost, so one line and one offer carry the totals.
        /// </summary>
        public async Task<ProfitLoss> SaveSimpleAsync(SimpleProfitLossDto dto)
        {
            var procurement =
                await _pnlRepository.GetProcurementWithJobTypeAsync(dto.ProcurementId)
                ?? throw new KeyNotFoundException($"Procurement {dto.ProcurementId} tidak ditemukan.");

            var jobTypeName =
                procurement.JobType?.TypeName
                ?? throw new InvalidOperationException("JobType tidak ditemukan untuk Procurement ini.");

            var current = await _pnlRepository.GetByProcurementAsync(dto.ProcurementId);
            var pnl = current == null
                ? new ProfitLoss { ProcurementId = dto.ProcurementId, NoLetterSelectedVendor = string.Empty }
                : await _pnlRepository.GetByIdAsync(current.ProfitLossId)
                    ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            var profit = dto.Revenue - dto.Cost;
            pnl.SelectedVendorId = dto.VendorId;
            pnl.SelectedVendorFinalOffer = dto.Cost;
            pnl.Profit = profit;
            pnl.ProfitPercent = dto.Revenue > 0 ? (profit / dto.Revenue) * 100m : 0m;
            pnl.AccrualAmount = dto.Revenue;
            pnl.RealizationAmount = dto.Cost;

            var item = pnl.Items.FirstOrDefault();
            if (item == null)
            {
                item = new ProfitLossItem
                {
                    ProfitLossId = pnl.ProfitLossId,
                    ProcOfferId = dto.ProcOfferId,
                    UnitQty = dto.UnitQty,
                    ItemName = "",
                };
                pnl.Items.Add(item);
            }
            item.Revenue = dto.Revenue;

            var offer = new VendorOffer
            {
                ProcurementId = dto.ProcurementId,
                ProfitLossId = pnl.ProfitLossId,
                VendorId = dto.VendorId,
                ProcOfferId = item.ProcOfferId,
                Round = 1,
                Price = dto.Cost,
                NoLetter = string.Empty,
                QuantityItem = 1,
                QuantityOfUnit = 1,
                UnitTypeId = _jobTypeCalc.GetVendorOfferUnitTypeId(jobTypeName, null),
            };

            if (current == null)
            {
                await _pnlRepository.StoreProfitLossAggregateAsync(pnl, [dto.VendorId], [offer]);
            }
            else
            {
                pnl.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
                await _pnlRepository.UpdateProfitLossAggregateAsync(pnl, [dto.VendorId], [offer]);
            }

            return pnl;
        }
    }
}
