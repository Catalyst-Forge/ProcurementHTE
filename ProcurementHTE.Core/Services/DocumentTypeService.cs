using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public DocumentTypeService(IDocumentTypeRepository documentTypeRepository) =>
            _documentTypeRepository = documentTypeRepository;

        public Task<PagedResult<Models.DocumentType>> GetAllDocumentTypesAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            return _documentTypeRepository.GetAllAsync(page, pageSize, search, fields, ct);
        }

        public async Task<DocumentType?> GetDocumentTypeByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            return await _documentTypeRepository.GetByIdAsync(id);
        }

        public async Task AddDocumentTypeAsync(Models.DocumentType documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            if (string.IsNullOrWhiteSpace(documentType.Name))
                throw new ArgumentException(
                    "Document type name cannot be empty",
                    nameof(documentType.Name)
                );

            if (documentType.Name.Length > 100)
                throw new ArgumentException(
                    "Document type name cannot exceed 100 characters",
                    nameof(documentType.Name)
                );

            await _documentTypeRepository.CreateDocumentTypeAsync(documentType);
        }

        public async Task EditDocumentTypeAsync(Models.DocumentType documentType, string id)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            if (string.IsNullOrWhiteSpace(documentType.Name))
                throw new ArgumentException(
                    "Document type name cannot be empty",
                    nameof(documentType.Name)
                );

            if (documentType.Name.Length > 100)
                throw new ArgumentException(
                    "Document type name cannot exceed 100 characters",
                    nameof(documentType.Name)
                );

            var existingDocumentType = await _documentTypeRepository.GetByIdAsync(id);
            if (existingDocumentType == null)
                throw new KeyNotFoundException($"Document type with ID '{id}' not found");

            existingDocumentType.Name = documentType.Name;
            existingDocumentType.Description = documentType.Description;

            await _documentTypeRepository.UpdateDocumentTypeAsync(existingDocumentType);
        }

        public async Task DeleteDocumentTypeAsync(Models.DocumentType documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            await _documentTypeRepository.DropDocumentTypeAsync(documentType);
        }
    }
}
