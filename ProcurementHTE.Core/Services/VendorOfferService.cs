using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class VendorOfferService : IVendorOfferService
    {
        private readonly IVendorOfferRepository _voRepository;

        public VendorOfferService(IVendorOfferRepository voRepository) =>
            _voRepository = voRepository;

        public async Task<IReadOnlyList<VendorOffer>> GetByWorkOrderAsync(string woId)
        {
            return await _voRepository.GetByWorkOrderAsync(woId);
        }
    }
}
