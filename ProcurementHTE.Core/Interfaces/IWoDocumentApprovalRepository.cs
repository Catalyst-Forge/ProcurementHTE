using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoDocumentApprovalRepository
    {
        Task<IReadOnlyList<WoDocumentApprovals>> GetByWoDocumentIdAsync(string woDocumentId);
        Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedByWoDocumentIdAsync(string woDocumentId);
        Task AddRangeAsync(IEnumerable<WoDocumentApprovals> rows);
        Task SaveChangesAsync();
    }
}
