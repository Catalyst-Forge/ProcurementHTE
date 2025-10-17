using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class VendorOfferService : IVendorOfferService {
        private readonly IVendorOfferRepository _voRepository;
        private readonly IWorkOrderRepository _woRepository;
        private readonly IVendorRepository _vendorRepository;

        public VendorOfferService(IVendorOfferRepository voRepository, IWorkOrderRepository woRepository, IVendorRepository vendorRepository) {
            _voRepository = voRepository;
            _woRepository = woRepository;
            _vendorRepository = vendorRepository;
        }

        public async Task<IEnumerable<VendorOffer>> GetOffersByWorkOrderAsync(string woId) {
            return await _voRepository.GetOffersByWorkOrderAsync(woId);
        }

        public async Task<decimal?> GetBestOfferPriceAsync(string woId){
            return await _voRepository.GetBestOfferPriceAsync(woId);
        }

        public async Task CreateVendorOfferAsync(IEnumerable<VendorOffer> vo) {

            await _voRepository.StoreVendorOfferAsync(vo);
        }

        public async Task<VendorOffer?> UpdateVendorOfferAsync(string id, decimal price) {
            var vendorOffer = await _voRepository.GetByIdWithDetailsAsync(id);
            if (vendorOffer == null)
                throw new InvalidOperationException("Vendor offer tidak ditemukan");

            vendorOffer.OfferPrice = price;
            return await _voRepository.UpdateVendorOfferAsync(vendorOffer);
        }

        public async Task DeleteVendorOfferAsync(string id) {
            await _voRepository.DropVendorOfferAsync(id);
        }
    }
}
