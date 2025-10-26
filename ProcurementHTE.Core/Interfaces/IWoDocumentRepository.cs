using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IWoDocumentRepository
{
    Task<WoDocuments?> GetByIdAsync(string id);
    Task<IReadOnlyList<WoDocuments>> GetByWorkOrderAsync(string workOrderId);
    Task<WoDocuments?> GetLatestActiveByWorkOrderAndDocTypeAsync(string woId, string documentTypeId);
    Task AddAsync(WoDocuments doc);
    Task UpdateAsync(WoDocuments doc);
    Task DeleteAsync(string id);
    Task SaveAsync();
}
