using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        public async Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId)
        {
            var pnl =
                await _pnlRepository.GetByIdAsync(profitLossId)
                ?? throw new KeyNotFoundException("Profit & Loss tidak ditemukan");

            var offers = await _voRepository.GetByProcurementAsync(pnl.ProcurementId);
            var roundLetters = await _roundLetterRepository.ListByProcurementAsync(
                pnl.ProcurementId
            );

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
                                IsIncluded = true,
                            };
                        })
                        .ToList(),
                })
                .ToList();

            var storedSelections = await _pnlRepository.GetSelectedVendorsAsync(
                pnl.ProcurementId
            );
            var selectedVendorIds =
                storedSelections.Count > 0
                    ? storedSelections.Select(x => x.VendorId).Distinct().ToList()
                    : vendors.Select(v => v.VendorId).Distinct().ToList();

            var items = pnl
                .Items.Select(item => new ProfitLossItemInputDto
                {
                    ProcOfferId = item.ProcOfferId,
                    Quantity = item.Quantity.HasValue ? (int)item.Quantity.Value : item.UnitQty,
                    QtyItems = item.UnitQty,
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
    }
}
