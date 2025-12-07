using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IVendorRoundLetterRepository
{
    Task<VendorRoundLetter?> GetAsync(string procurementId, string vendorId, int round);
    Task<IReadOnlyList<VendorRoundLetter>> ListByProcurementAsync(string procurementId);
    Task AddOrUpdateAsync(VendorRoundLetter entity);
    Task UpdateProfitLossLinkAsync(
        string procurementId,
        string vendorId,
        int round,
        string? profitLossId,
        string? letterNumber
    );
    Task DeleteByProcDocumentIdAsync(string procDocumentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
