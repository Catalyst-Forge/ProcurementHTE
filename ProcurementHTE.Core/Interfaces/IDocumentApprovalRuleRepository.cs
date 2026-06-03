using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDocumentApprovalRuleRepository
    {
        Task<List<DocumentApprovalRule>> GetAllAsync(string? documentTypeId, CancellationToken ct = default);
        Task<DocumentApprovalRule?> GetByIdAsync(string id, CancellationToken ct = default);
        Task AddAsync(DocumentApprovalRule rule, CancellationToken ct = default);
        Task UpdateAsync(DocumentApprovalRule rule, CancellationToken ct = default);
        Task DeleteAsync(DocumentApprovalRule rule, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);

        Task<List<DocumentApprovalRule>> GetActiveByDocNameAsync(
            string documentName,
            string? jobTypeId,
            Core.Enums.ProcurementCategory? category,
            CancellationToken ct = default
        );

        Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default);
        Task<List<JobTypes>> GetJobTypesAsync(CancellationToken ct = default);
    }
}
