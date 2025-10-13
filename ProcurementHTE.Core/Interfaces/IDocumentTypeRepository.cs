using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentTypeRepository
    {
        Task<IEnumerable<DocumentType>> GetAllAsync();
        Task<DocumentType?> GetByIdAsync(int id);
        Task CreateDocumentTypeAsync(DocumentType documentType);
        Task UpdateDocumentTypeAsync(DocumentType documentType);
        Task DropDocumentTypeAsync(DocumentType documentType);

    }
}
