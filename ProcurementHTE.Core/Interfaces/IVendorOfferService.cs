using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IVendorOfferService
    {
        Task<IReadOnlyList<VendorOffer>> GetByProcurementAsync(string procurementId);
    }
}
