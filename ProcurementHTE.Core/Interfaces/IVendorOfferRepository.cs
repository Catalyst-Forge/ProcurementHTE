using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IVendorOfferRepository
    {
        // Get Data
        Task<IReadOnlyList<VendorOffer>> GetByProcurementAsync(string procurementId);

        // Transaction DB>
        Task StoreAllOffersAsync(IEnumerable<VendorOffer> offers);
        Task RemoveByProcurementAsync(string procurementId);
    }
}
