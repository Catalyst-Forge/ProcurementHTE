using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class ProcDocumentApprovalService : IProcDocumentApprovalService {
        private readonly IProcDocumentApprovalRepository _repository;
        private readonly ILogger<ProcDocumentApprovalService> _logger;

        public ProcDocumentApprovalService(IProcDocumentApprovalRepository repository, ILogger<ProcDocumentApprovalService> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedDocumentsAsync(string  procDocumentId) {
            try {
                if (string.IsNullOrWhiteSpace(procDocumentId)) {
                    _logger.LogWarning("GetApprovedDocuments called with empty procDocumentId");
                    return new List<ProcDocumentApprovals>();
                }

                var approvals = await _repository.GetApprovedByProcDocumentIdAsync(procDocumentId);
                _logger.LogInformation("Retrieved {Count} approved documents for ProcDocumentId: {ProcDocumentId}",
                approvals.Count, procDocumentId);

                return approvals;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error retrieving approved docuemnts for ProcDocumentId: {ProcDocumentId}", procDocumentId);
                throw;
            }
        }
    }
}
