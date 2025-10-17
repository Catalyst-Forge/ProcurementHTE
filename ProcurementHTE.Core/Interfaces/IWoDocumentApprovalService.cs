using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IWoDocumentApprovalService {
        Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedDocumentsAsync(string woDocumentId);
    }
}
