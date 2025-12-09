using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public class DocumentApprovalsService : IDocumentApprovalsService
{
    private readonly IDocumentApprovalsRepository _repository;

    public DocumentApprovalsService(IDocumentApprovalsRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<DocumentApprovals>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    )
    {
        return _repository.GetAllAsync(jobTypeId, ct);
    }

    public Task<DocumentApprovals?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _repository.GetByIdAsync(id, ct);
    }

    public Task CreateAsync(DocumentApprovals model, CancellationToken ct = default)
    {
        return _repository.CreateAsync(model, ct);
    }

    public Task UpdateAsync(DocumentApprovals model, CancellationToken ct = default)
    {
        return _repository.UpdateAsync(model, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        return _repository.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<JobTypeDocuments>> GetJobTypeDocumentsAsync(
        CancellationToken ct = default
    )
    {
        return _repository.GetJobTypeDocumentsAsync(ct);
    }

    public Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default)
    {
        return _repository.GetRolesAsync(ct);
    }
}
