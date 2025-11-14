using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IPdfGenerator {
        Task<byte[]> GenerateProfitLossPdfAsync(ProfitLoss pnl, Procurement wo, Vendor? selectedVendor, IReadOnlyList<VendorOffer> offers);
    }
}