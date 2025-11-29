using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class JobTypeDocumentService : IJobTypeDocumentService
    {
        private readonly IJobTypeDocumentRepository _repository;

        public JobTypeDocumentService(IJobTypeDocumentRepository repository)
        {
            _repository = repository;
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
                throw new InvalidOperationException(
                    $"Gagal mengambil konfigurasi dokumen untuk job type '{jobTypeId}': {ex.Message}",
                    ex
                );
            }
        }
    }
}
