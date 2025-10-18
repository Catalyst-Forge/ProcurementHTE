using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentTypeRepository
    {
        Task<PagedResult<DocumentType>> GetAllAsync(int page, int pageSize, CancellationToken ct);
        Task<DocumentType?> GetByIdAsync(string id);
        Task CreateDocumentTypeAsync(DocumentType documentType);
        Task UpdateDocumentTypeAsync(DocumentType documentType);
        Task DropDocumentTypeAsync(DocumentType documentType);
    }
}
