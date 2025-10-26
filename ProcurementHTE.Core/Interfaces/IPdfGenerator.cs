using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IPdfGenerator {
        Task<byte[]> GenerateProfitLossPdfAsync(ProfitLoss pnl, WorkOrder wo, Vendor? selectedVendor, IReadOnlyList<VendorOffer> offers);
    }
}