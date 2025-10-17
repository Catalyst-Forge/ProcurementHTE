using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IVendorOfferRepository {
        // Get Data
        Task<IEnumerable<VendorOffer>> GetAllVendorOffersAsync();
        Task<IEnumerable<VendorOffer>> GetOffersByWorkOrderAsync(string woId);
        Task<VendorOffer?> GetByIdWithDetailsAsync(string id);
        Task<decimal?> GetBestOfferPriceAsync(string woId);

        // Transaction DB>
        Task StoreVendorOfferAsync(IEnumerable<VendorOffer> vo);
        Task<VendorOffer?> UpdateVendorOfferAsync(VendorOffer vo);
        Task DropVendorOfferAsync(string id);
    }
}
