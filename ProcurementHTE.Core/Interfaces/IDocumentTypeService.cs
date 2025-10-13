using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<IEnumerable<DocumentType>> GetAllDocumentTypesAsync();
        Task<DocumentType?> GetDocumentTypeByIdAsync(int id);
        Task AddDocumentTypeAsync(DocumentType documentType);
        Task EditDocumentTypeAsync(DocumentType documentType, int id);
        Task DeleteDocumentTypeAsync(DocumentType documentType);
    }
}
