using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class ProcDocumentApprovalService : IProcDocumentApprovalService
    {
        private readonly IProcDocumentApprovalRepository _repository;
        private readonly ILogger<ProcDocumentApprovalService> _logger;

        public ProcDocumentApprovalService(
            IProcDocumentApprovalRepository repository,
            ILogger<ProcDocumentApprovalService> logger
        )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProcDocumentApprovals>> GetApprovedDocumentsAsync(
            string procDocumentId
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(procDocumentId))
                {
                    return new List<ProcDocumentApprovals>();
                }

                var approvals = await _repository.GetApprovedByProcDocumentIdAsync(procDocumentId);

                return approvals;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get approved documents for ProcDocument {ProcDocumentId}",
                    procDocumentId
                );
                throw;
            }
        }
    }
}
