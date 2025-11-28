using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class JobTypeDocumentService : IJobTypeDocumentService
    {
        private readonly IJobTypeDocumentRepository _repository;
        private readonly ILogger<JobTypeDocumentService> _logger;

        public JobTypeDocumentService(
            IJobTypeDocumentRepository repository,
            ILogger<JobTypeDocumentService> logger
        )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<JobTypeDocuments?> GetRequiredDocumentAsync(
            string jobTypeId,
            string documentTypeId
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobTypeId))
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(documentTypeId))
                {
                    return null;
                }

                var jobTypeDocument = await _repository.FindByJobTypeAndDocTypeAsync(
                    jobTypeId,
                    documentTypeId
                );
                if (jobTypeDocument == null)
                {
                    return null;
                }

                return jobTypeDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get required document for JobType {JobTypeId} and DocumentType {DocumentTypeId}",
                    jobTypeId,
                    documentTypeId
                );
                throw;
            }
        }
    }
}
