using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoDocumentApprovalRepository
    {
        Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedByWoDocumentIdAsync(string woDocumentId);
    }
}
