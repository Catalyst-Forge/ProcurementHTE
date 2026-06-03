using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class DocumentApprovalRuleService : IDocumentApprovalRuleService
    {
        private readonly IDocumentApprovalRuleRepository _repo;

        public DocumentApprovalRuleService(IDocumentApprovalRuleRepository repo)
        {
            _repo = repo;
        }

        public Task<List<DocumentApprovalRule>> GetAllAsync(string? documentTypeId, CancellationToken ct = default) =>
            _repo.GetAllAsync(documentTypeId, ct);

        public Task<DocumentApprovalRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
            _repo.GetByIdAsync(id, ct);

        public async Task CreateAsync(DocumentApprovalRule rule, CancellationToken ct = default)
        {
            await _repo.AddAsync(rule, ct);
            await _repo.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(DocumentApprovalRule rule, CancellationToken ct = default)
        {
            await _repo.UpdateAsync(rule, ct);
            await _repo.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, ct);
            if (entity == null)
                return;
            await _repo.DeleteAsync(entity, ct);
            await _repo.SaveChangesAsync(ct);
        }

        public Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default) =>
            _repo.GetDocumentTypesAsync(ct);

        public Task<List<JobTypes>> GetJobTypesAsync(CancellationToken ct = default) =>
            _repo.GetJobTypesAsync(ct);
    }
}
