using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IJobTypeDocumentService {
        Task<JobTypeDocuments?> GetRequiredDocumentAsync(string jobTypeId, string documentTypeId);
    }
}
