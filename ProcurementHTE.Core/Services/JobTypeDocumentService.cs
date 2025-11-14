using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class JobTypeDocumentService : IJobTypeDocumentService {
        private readonly IJobTypeDocumentRepository _repository;
        private readonly ILogger<JobTypeDocumentService> _logger;

        public JobTypeDocumentService(IJobTypeDocumentRepository repository, ILogger<JobTypeDocumentService> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<JobTypeDocuments?> GetRequiredDocumentAsync(string jobTypeId, string documentTypeId) {
            try {
                if (string.IsNullOrWhiteSpace(jobTypeId)) {
                    _logger.LogWarning("GetRequiredDocument called with empty jobTypeId");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(documentTypeId)) {
                    _logger.LogWarning("GetRequiredDocument called with empty documentTypeId");
                    return null;
                }

                var jobTypeDocument = await _repository.FindByJobTypeAndDocTypeAsync(jobTypeId, documentTypeId);
                if (jobTypeDocument == null) {
                    _logger.LogInformation("No JobTypeDocument found for JobTypeId: {JobTypeId} and DocumentTypeId: {DocumentTypeId}",
                    jobTypeId, documentTypeId);
                    return null;
                }

                _logger.LogInformation("Retrieved JobTypeDocument - JobType: {JobTypeName}, DocumentType: {DocumentTypeName}, IsRequired: {IsRequired}",
                jobTypeDocument.JobType?.JobTypeId, jobTypeDocument.DocumentType?.DocumentTypeId, jobTypeDocument.IsMandatory);

                return jobTypeDocument;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error retrieving JobTypeDocument for JobTypeId: {JobTypeId} and DocumentTypeId: {DocumentTypeId}", jobTypeId, documentTypeId);
                throw;
            }
        }
    }
}
