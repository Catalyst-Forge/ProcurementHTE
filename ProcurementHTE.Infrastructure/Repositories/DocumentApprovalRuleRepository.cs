using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class DocumentApprovalRuleRepository : IDocumentApprovalRuleRepository
    {
        private readonly AppDbContext _db;

        public DocumentApprovalRuleRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<DocumentApprovalRule>> GetAllAsync(
            string? documentTypeId,
            CancellationToken ct = default
        )
        {
            var query = _db.DocumentApprovalRules
                .Include(r => r.DocumentType)
                .Include(r => r.JobType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(documentTypeId))
                query = query.Where(r => r.DocumentTypeId == documentTypeId);

            return await query
                .OrderBy(r => r.DocumentType!.Name)
                .ThenBy(r => r.MinAmount)
                .ThenBy(r => r.Sequence)
                .ToListAsync(ct);
        }

        public Task<DocumentApprovalRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
            _db.DocumentApprovalRules
                .Include(r => r.DocumentType)
                .Include(r => r.JobType)
                .FirstOrDefaultAsync(r => r.DocumentApprovalRuleId == id, ct);

        public async Task AddAsync(DocumentApprovalRule rule, CancellationToken ct = default) =>
            await _db.DocumentApprovalRules.AddAsync(rule, ct);

        public Task UpdateAsync(DocumentApprovalRule rule, CancellationToken ct = default)
        {
            _db.DocumentApprovalRules.Update(rule);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(DocumentApprovalRule rule, CancellationToken ct = default)
        {
            _db.DocumentApprovalRules.Remove(rule);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken ct = default) =>
            _db.DocumentTypes.OrderBy(d => d.Name).ToListAsync(ct);

        public Task<List<JobTypes>> GetJobTypesAsync(CancellationToken ct = default) =>
            _db.JobTypes.OrderBy(j => j.TypeName).ToListAsync(ct);

        public async Task<List<DocumentApprovalRule>> GetActiveByDocNameAsync(
            string documentName,
            string? jobTypeId,
            Core.Enums.ProcurementCategory? category,
            CancellationToken ct = default
        )
        {
            var query = _db.DocumentApprovalRules
                .Include(r => r.DocumentType)
                .Include(r => r.JobType)
                .Where(r => r.IsActive);

            if (!string.IsNullOrWhiteSpace(documentName))
                query = query.Where(r => r.DocumentType!.Name == documentName);

            if (!string.IsNullOrWhiteSpace(jobTypeId))
                query = query.Where(r => r.JobTypeId == null || r.JobTypeId == jobTypeId);

            if (category.HasValue)
                query = query.Where(
                    r => r.ProcurementCategory == null || r.ProcurementCategory == category.Value
                );

            return await query.OrderBy(r => r.MinAmount).ThenBy(r => r.Sequence).ToListAsync(ct);
        }
    }
}
