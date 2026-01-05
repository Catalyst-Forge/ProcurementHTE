using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class VendorOfferRepository : IVendorOfferRepository
    {
        private readonly AppDbContext _context;

        public VendorOfferRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IReadOnlyList<VendorOffer>> GetByProcurementAsync(string procurementId)
        {
            return await _context
                .VendorOffers.Where(offer => offer.ProcurementId == procurementId)
                .OrderBy(offer => offer.VendorId)
                .ThenBy(offer => offer.Round)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task StoreAllOffersAsync(IEnumerable<VendorOffer> offers)
        {
            await _context.VendorOffers.AddRangeAsync(offers);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveByProcurementAsync(string procurementId)
        {
            // Soft delete: mark all vendor offers for this procurement as deleted
            var olds = await _context
                .VendorOffers.Where(offer => offer.ProcurementId == procurementId)
                .ToListAsync();
            if (olds.Count > 0)
            {
                foreach (var offer in olds)
                {
                    offer.IsDeleted = true;
                    offer.DeletedAt = DateTime.UtcNow;
                    // Note: DeletedBy should be set by the service layer with current user ID
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
