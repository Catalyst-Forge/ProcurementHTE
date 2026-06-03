using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class DocumentApprovalsRepository : IDocumentApprovalsRepository
{
    private readonly AppDbContext _db;
    private readonly RoleManager<Role> _roleManager;

    public DocumentApprovalsRepository(AppDbContext db, RoleManager<Role> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<DocumentApprovals>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    )
    {
        var approvals = _db
            .DocumentApprovals.Include(x => x.JobTypeDocument)
            .ThenInclude(j => j.JobType)
            .Include(x => x.JobTypeDocument)
            .ThenInclude(j => j.DocumentType)
            .Include(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobTypeId))
        {
            approvals = approvals.Where(x => x.JobTypeDocument.JobTypeId == jobTypeId);
        }

        return await approvals
            .OrderBy(x => x.JobTypeDocument.JobType.TypeName)
            .ThenBy(x => x.JobTypeDocument.DocumentType.Name)
            .ThenBy(x => x.Level)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<DocumentApprovals?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _db.DocumentApprovals.FindAsync(new object?[] { id }, ct).AsTask();
    }

    public async Task CreateAsync(DocumentApprovals model, CancellationToken ct = default)
    {
        _db.DocumentApprovals.Add(model);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DocumentApprovals model, CancellationToken ct = default)
    {
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.DocumentApprovals.FindAsync(new object?[] { id }, ct);
        if (entity is null)
            return;

        _db.DocumentApprovals.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<JobTypeDocuments>> GetJobTypeDocumentsAsync(
        CancellationToken ct = default
    )
    {
        return await _db
            .JobTypeDocuments.Include(x => x.JobType)
            .Include(x => x.DocumentType)
            .OrderBy(x => x.JobType.TypeName)
            .ThenBy(x => x.DocumentType.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(ct);
    }
}
