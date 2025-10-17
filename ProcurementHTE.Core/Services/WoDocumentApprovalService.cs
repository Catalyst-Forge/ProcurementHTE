using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class WoDocumentApprovalService : IWoDocumentApprovalService {
        private readonly IWoDocumentApprovalRepository _repository;
        private readonly ILogger<WoDocumentApprovalService> _logger;

        public WoDocumentApprovalService(IWoDocumentApprovalRepository repository, ILogger<WoDocumentApprovalService> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetApprovedDocumentsAsync(string  woDocumentId) {
            try {
                if (string.IsNullOrWhiteSpace(woDocumentId)) {
                    _logger.LogWarning("GetApprovedDocuments called with empty woDocumentId");
                    return new List<WoDocumentApprovals>();
                }

                var approvals = await _repository.GetApprovedByWoDocumentIdAsync(woDocumentId);
                _logger.LogInformation("Retrieved {Count} approved documents for WoDocumentId: {WoDocumentId}",
                approvals.Count, woDocumentId);

                return approvals;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error retrieving approved docuemnts for WoDocumentId: {WoDocumentId}", woDocumentId);
                throw;
            }
        }
    }
}
