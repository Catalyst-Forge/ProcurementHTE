using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProcDocumentApprovalService
    {
        Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedDocumentsAsync(string procDocumentId);
    }
}
