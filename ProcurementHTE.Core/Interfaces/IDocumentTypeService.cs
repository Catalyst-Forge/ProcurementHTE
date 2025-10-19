using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<PagedResult<DocumentType>> GetAllDocumentTypesAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        );
        Task<DocumentType?> GetDocumentTypeByIdAsync(string id);
        Task AddDocumentTypeAsync(DocumentType documentType);
        Task EditDocumentTypeAsync(DocumentType documentType, string id);
        Task DeleteDocumentTypeAsync(DocumentType documentType);
    }
}
