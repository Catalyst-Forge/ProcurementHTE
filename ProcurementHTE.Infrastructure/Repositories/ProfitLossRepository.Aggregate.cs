using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProfitLossRepository
    {
        public async Task StoreProfitLossAggregateAsync(
            ProfitLoss profitLoss,
            IEnumerable<string> selectedVendorIds,
            IEnumerable<VendorOffer> vendorOffers
        )
        {
            ArgumentNullException.ThrowIfNull(profitLoss);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RemoveExistingSelectedVendorsAsync(profitLoss.ProcurementId);
                await AddSelectedVendorsAsync(profitLoss.ProcurementId, selectedVendorIds);

                await _context.ProfitLosses.AddAsync(profitLoss);

                var offers = vendorOffers?.ToList() ?? [];
                if (offers.Count > 0)
                    await _context.VendorOffers.AddRangeAsync(offers);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateProfitLossAggregateAsync(
            ProfitLoss profitLoss,
            IEnumerable<string> selectedVendorIds,
            IEnumerable<VendorOffer> vendorOffers
        )
        {
            ArgumentNullException.ThrowIfNull(profitLoss);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await RemoveExistingSelectedVendorsAsync(profitLoss.ProcurementId);
                await AddSelectedVendorsAsync(profitLoss.ProcurementId, selectedVendorIds);

                var oldOffers = await _context
                    .VendorOffers.Where(o => o.ProcurementId == profitLoss.ProcurementId)
                    .ToListAsync();
                if (oldOffers.Count > 0)
                    _context.VendorOffers.RemoveRange(oldOffers);

                var offers = vendorOffers?.ToList() ?? [];
                if (offers.Count > 0)
                    await _context.VendorOffers.AddRangeAsync(offers);

                _context.ProfitLosses.Update(profitLoss);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task RemoveExistingSelectedVendorsAsync(string procurementId)
        {
            var existing = await _context
                .ProfitLossSelectedVendors.Where(item => item.ProcurementId == procurementId)
                .ToListAsync();

            if (existing.Count > 0)
                _context.ProfitLossSelectedVendors.RemoveRange(existing);
        }

        private Task AddSelectedVendorsAsync(string procurementId, IEnumerable<string> selectedIds)
        {
            var rows = selectedIds
                .Distinct()
                .Select(vendor => new ProfitLossSelectedVendor
                {
                    ProcurementId = procurementId,
                    VendorId = vendor,
                })
                .ToList();

            if (rows.Count == 0)
                return Task.CompletedTask;

            return _context.ProfitLossSelectedVendors.AddRangeAsync(rows);
        }

        public async Task UpdateProcOfferUnitRevenueAsync(string procOfferId, string unitRevenue)
        {
            var procOffer = await _context.ProcOffers.FirstOrDefaultAsync(o => o.ProcOfferId == procOfferId);
            if (procOffer != null)
            {
                procOffer.UnitRevenue = unitRevenue;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(string profitLossId, string deletedByUserId)
        {
            var entityToDelete = await _context.ProfitLosses.FirstOrDefaultAsync(
                p => p.ProfitLossId == profitLossId
            );

            if (entityToDelete != null)
            {
                entityToDelete.IsDeleted = true;
                entityToDelete.DeletedAt = DateTime.UtcNow;
                entityToDelete.DeletedBy = deletedByUserId;

                await _context.SaveChangesAsync();
            }
        }
    }
}
