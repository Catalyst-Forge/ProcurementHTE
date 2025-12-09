using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IJobTypeDocumentAdminService
{
    Task<IReadOnlyList<JobTypeDocuments>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    );

    Task<JobTypeDocuments?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<bool> ExistsAsync(
        string jobTypeId,
        string documentTypeId,
        ProcurementCategory? procurementCategory,
        string? excludeId,
        CancellationToken ct = default
    );

    Task CreateAsync(JobTypeDocuments model, CancellationToken ct = default);

    Task UpdateAsync(JobTypeDocuments model, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<JobTypes>> GetJobTypesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default);
}
