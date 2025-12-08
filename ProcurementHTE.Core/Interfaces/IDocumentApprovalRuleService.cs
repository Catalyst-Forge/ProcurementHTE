using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentApprovalRuleService
    {
        Task<List<DocumentApprovalRule>> GetAllAsync(string? documentTypeId, CancellationToken ct = default);
        Task<DocumentApprovalRule?> GetByIdAsync(string id, CancellationToken ct = default);
        Task CreateAsync(DocumentApprovalRule rule, CancellationToken ct = default);
        Task UpdateAsync(DocumentApprovalRule rule, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);

        Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default);
        Task<List<JobTypes>> GetJobTypesAsync(CancellationToken ct = default);
    }
}
