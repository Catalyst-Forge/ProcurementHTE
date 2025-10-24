using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoTypeDocumentRepository
    {
        Task<WoTypeDocuments?> FindByWoTypeAndDocTypeAsync(string woTypeId, string documentTypeId);
        Task<WoTypeDocuments?> GetByWoTypeAndDocumentTypeAsync(string woTypeId, string documentTypeId);
        Task<IReadOnlyList<WoTypeDocuments>> ListByWoTypeAsync(string woTypeId);
    }
}
