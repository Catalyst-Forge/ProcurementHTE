using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IWoDocumentRepository
    {
        Task<WoDocuments?> GetByIdWithWorkOrderAsync(string woDocumentId);
    }
}
