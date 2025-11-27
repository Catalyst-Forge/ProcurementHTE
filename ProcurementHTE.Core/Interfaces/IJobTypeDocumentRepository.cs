using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    // Interface (sudah OK)
    public interface IJobTypeDocumentRepository
    {
        Task<JobTypeDocuments?> FindByJobTypeAndDocTypeAsync(
            string jobTypeId,
            string documentTypeId
        );
        Task<JobTypeDocuments?> GetByJobTypeAndDocumentTypeAsync(
            string jobTypeId,
            string documentTypeId
        );
        Task<IReadOnlyList<JobTypeDocuments>> ListByJobTypeAsync(
            string jobTypeId,
            CancellationToken ct = default
        );
    }
}
