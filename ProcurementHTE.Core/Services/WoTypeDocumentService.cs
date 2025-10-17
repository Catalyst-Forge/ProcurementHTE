using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class WoTypeDocumentService : IWoTypeDocumentService {
        private readonly IWoTypeDocumentRepository _repository;
        private readonly ILogger<WoTypeDocumentService> _logger;

        public WoTypeDocumentService(IWoTypeDocumentRepository repository, ILogger<WoTypeDocumentService> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<WoTypeDocuments?> GetRequiredDocumentAsync(string woTypeId, string documentTypeId) {
            try {
                if (string.IsNullOrWhiteSpace(woTypeId)) {
                    _logger.LogWarning("GetRequiredDocument called with empty woTypeId");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(documentTypeId)) {
                    _logger.LogWarning("GetRequiredDocument called with empty documentTypeId");
                    return null;
                }

                var woTypeDocument = await _repository.FindByWoTypeAndDocTypeAsync(woTypeId, documentTypeId);
                if (woTypeDocument == null) {
                    _logger.LogInformation("No WoTypeDocument found for WoTypeId: {WoTypeId} and DocumentTypeId: {DocumentTypeId}",
                    woTypeId, documentTypeId);
                    return null;
                }

                _logger.LogInformation("Retrieved WoTypeDocument - WoType: {WoTypeName}, DocumentType: {DocumentTypeName}, IsRequired: {IsRequired}",
                woTypeDocument.WoType?.WoTypeId, woTypeDocument.DocumentType?.DocumentTypeId, woTypeDocument.IsMandatory);

                return woTypeDocument;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error retrieving WoTypeDocument for WoTypeId: {WoTypeId} and DocumentTypeId: {DocumentTypeId}", woTypeId, documentTypeId);
                throw;
            }
        }
    }
}
