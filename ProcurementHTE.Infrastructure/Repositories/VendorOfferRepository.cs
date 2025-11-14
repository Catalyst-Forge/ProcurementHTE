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

        public async Task<IReadOnlyList<VendorOffer>> GetByProcurementAsync(string woId)
        {
            return await _context
                .VendorOffers
                .Where(offer => offer.ProcurementId == woId)
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

        public async Task RemoveByProcurementAsync(string woId)
        {
            var olds = await _context
                .VendorOffers.Where(offer => offer.ProcurementId == woId)
                .ToListAsync();
            if (olds.Count > 0)
                _context.RemoveRange(olds);
        }
    }
}
