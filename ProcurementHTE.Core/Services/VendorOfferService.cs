using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class VendorOfferService : IVendorOfferService
    {
        private readonly IVendorOfferRepository _voRepository;

        public VendorOfferService(IVendorOfferRepository voRepository) =>
            _voRepository = voRepository;

        public async Task<IReadOnlyList<VendorOffer>> GetByProcurementAsync(string procurementId)
        {
            return await _voRepository.GetByProcurementAsync(procurementId);
        }
    }
}
