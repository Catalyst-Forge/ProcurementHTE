using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class VendorOfferRepository : IVendorOfferRepository
    {
        private readonly AppDbContext _context;

        public VendorOfferRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IEnumerable<VendorOffer>> GetAllVendorOffersAsync()
        {
            return await _context
                .VendorOffers.Include(vo => vo.Vendor)
                .Include(vo => vo.WorkOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<VendorOffer>> GetOffersByWorkOrderAsync(string woId)
        {
            return await _context.VendorOffers
                .Where(vo => vo.WorkOrderId == woId)
                .Include(vo => vo.Vendor)
                .OrderBy(vo => vo.OfferNumber)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<VendorOffer?> GetByIdWithDetailsAsync(string id)
        {
            return await _context
                .VendorOffers.Include(vo => vo.Vendor)
                .Include(vo => vo.WorkOrder)
                .FirstOrDefaultAsync(vo => vo.VendorOfferId == id);
        }

        public async Task<decimal?> GetBestOfferPriceAsync(string woId)
        {
            var offers = await _context
                .VendorOffers.Where(vo => vo.WorkOrderId == woId)
                .ToListAsync();

            return offers.Count != 0 ? offers.Min(offer => offer.OfferPrice) : 0;
        }

        public async Task StoreVendorOfferAsync(IEnumerable<VendorOffer> vo)
        {
            await _context.VendorOffers.AddRangeAsync(vo);
            await _context.SaveChangesAsync();
        }

        public async Task<VendorOffer?> UpdateVendorOfferAsync(VendorOffer vo)
        {
            _context.VendorOffers.Update(vo);
            await _context.SaveChangesAsync();
            return await GetByIdWithDetailsAsync(vo.VendorOfferId);
        }

        public async Task DropVendorOfferAsync(string id)
        {
            var offerExist = await _context.VendorOffers.FindAsync(id);
            if (offerExist != null)
            {
                _context.VendorOffers.Remove(offerExist);
                await _context.SaveChangesAsync();
            }
        }
    }
}
