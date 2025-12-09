using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IDocumentApprovalsRepository
{
    Task<IReadOnlyList<DocumentApprovals>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    );

    Task<DocumentApprovals?> GetByIdAsync(string id, CancellationToken ct = default);

    Task CreateAsync(DocumentApprovals model, CancellationToken ct = default);

    Task UpdateAsync(DocumentApprovals model, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<JobTypeDocuments>> GetJobTypeDocumentsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);
}
