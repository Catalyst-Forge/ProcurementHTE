using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IWoTypeDocumentService {
        Task<WoTypeDocuments?> GetRequiredDocumentAsync(string woTypeId, string documentTypeId);
    }
}
