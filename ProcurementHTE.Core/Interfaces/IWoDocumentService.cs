using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IWoDocumentService {
        Task<WoDocuments?> GetDocumentWithWorkOrderAsync(string woDocumentId);
    }
}
