using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class WoDocumentService : IWoDocumentService {
        private readonly IWoDocumentRepository _repository;
        private readonly ILogger<WoDocumentService> _logger;

        public WoDocumentService(IWoDocumentRepository repository, ILogger<WoDocumentService> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<WoDocuments?> GetDocumentWithWorkOrderAsync(string woDocumentId) {
            try {
                if (string.IsNullOrWhiteSpace(woDocumentId)) {
                    _logger.LogWarning("GetDocumentWithWorkOrder called with empty woDocumentId");
                    return null;
                }

                var document = await _repository.GetByIdWithWorkOrderAsync(woDocumentId);
                if (document == null) {
                    _logger.LogInformation("Document not found for WoDocumentId: {WoDocumentId}", woDocumentId);
                    return null;
                }

                _logger.LogInformation("Retrieved document {DocumentId} with WorkOrder {WorkOrderId}", document.WoDocumentId, document.WorkOrder?.WorkOrderId);
                return document;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error retrieving document with work order for WoDocumentId: {WoDocumentId}", woDocumentId);
                throw;
            }
        }
    }
}
