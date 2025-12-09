using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public class JobTypeDocumentAdminService : IJobTypeDocumentAdminService
{
    private readonly IJobTypeDocumentAdminRepository _repository;

    public JobTypeDocumentAdminService(IJobTypeDocumentAdminRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<JobTypeDocuments>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    )
    {
        return _repository.GetAllAsync(jobTypeId, ct);
    }

    public Task<JobTypeDocuments?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _repository.GetByIdAsync(id, ct);
    }

    public Task<bool> ExistsAsync(
        string jobTypeId,
        string documentTypeId,
        ProcurementCategory? procurementCategory,
        string? excludeId,
        CancellationToken ct = default
    )
    {
        return _repository.ExistsAsync(
            jobTypeId,
            documentTypeId,
            procurementCategory,
            excludeId,
            ct
        );
    }

    public Task CreateAsync(JobTypeDocuments model, CancellationToken ct = default)
    {
        return _repository.CreateAsync(model, ct);
    }

    public Task UpdateAsync(JobTypeDocuments model, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(model, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        return _repository.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<JobTypes>> GetJobTypesAsync(CancellationToken ct = default)
    {
        return _repository.GetJobTypesAsync(ct);
    }

    public Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default)
    {
        return _repository.GetDocumentTypesAsync(ct);
    }
}
