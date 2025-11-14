using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcDocumentApprovalRepository
    {
        Task<IReadOnlyList<ProcDocumentApprovals>> GetByProcDocumentIdAsync(string procDocumentId);
        Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedByProcDocumentIdAsync(string procDocumentId);
        Task AddRangeAsync(IEnumerable<ProcDocumentApprovals> rows);
        Task SaveChangesAsync();
    }
}
