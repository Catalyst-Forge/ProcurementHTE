using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IVendorOfferService {
        Task<IEnumerable<VendorOffer>> GetOffersByWorkOrderAsync(string woId);
        Task<decimal?> GetBestOfferPriceAsync(string woId);

        Task CreateVendorOfferAsync(IEnumerable<VendorOffer> vo);
        Task<VendorOffer?> UpdateVendorOfferAsync(string id, decimal price);
        Task DeleteVendorOfferAsync(string id);
    }
}
