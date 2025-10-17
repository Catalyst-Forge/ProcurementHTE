using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;
        public DocumentTypeService(IDocumentTypeRepository documentTypeRepository) 
        {
            _documentTypeRepository = documentTypeRepository;
        }

        public async Task<IEnumerable<Models.DocumentType>> GetAllDocumentTypesAsync()
        {
            return await _documentTypeRepository.GetAllAsync();
        }

        public async Task AddDocumentTypeAsync(Models.DocumentType documentType)
        {
            if (string.IsNullOrEmpty(documentType.Name))
            {
                throw new ArgumentException("Document Name cannot be empty");
            }
            await _documentTypeRepository.CreateDocumentTypeAsync(documentType);
        }

        public async Task EditDocumentTypeAsync(Models.DocumentType documentType, string id)
        {
            if (documentType == null)
            {
                throw new ArgumentNullException(nameof(documentType));
            }
            var existingDocumentType = await _documentTypeRepository.GetByIdAsync(id);
            if (existingDocumentType == null)
            {
                throw new KeyNotFoundException($"Document Type With ID {id} not found");
            }
            existingDocumentType.Name = documentType.Name;
            existingDocumentType.Description = documentType.Description;
            await _documentTypeRepository.UpdateDocumentTypeAsync(existingDocumentType);
        }

        public async Task DeleteDocumentTypeAsync(Models.DocumentType documentType)
        {
            if (documentType == null)
            {
                throw new ArgumentNullException(nameof(documentType));
            }
            await _documentTypeRepository.DropDocumentTypeAsync(documentType);
        }

        public Task<DocumentType?> GetDocumentTypeByIdAsync(string id)
        {
            return _documentTypeRepository.GetByIdAsync(id);
        }
    }
}
