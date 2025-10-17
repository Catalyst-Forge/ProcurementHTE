using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<IEnumerable<DocumentType>> GetAllDocumentTypesAsync();
        Task<DocumentType?> GetDocumentTypeByIdAsync(string id);
        Task AddDocumentTypeAsync(DocumentType documentType);
        Task EditDocumentTypeAsync(DocumentType documentType, string id);
        Task DeleteDocumentTypeAsync(DocumentType documentType);
    }
}
