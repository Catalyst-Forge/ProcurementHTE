using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IVendorOfferRepository
    {
        // Get Data
        Task<IReadOnlyList<VendorOffer>> GetByWorkOrderAsync(string woId);

        // Transaction DB>
        Task StoreAllOffersAsync(IEnumerable<VendorOffer> offers);
        Task RemoveByWorkOrderAsync(string woId);
    }
}
