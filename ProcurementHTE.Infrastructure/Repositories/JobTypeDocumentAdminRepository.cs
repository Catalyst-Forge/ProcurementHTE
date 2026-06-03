using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class JobTypeDocumentAdminRepository : IJobTypeDocumentAdminRepository
{
    private readonly AppDbContext _db;

    public JobTypeDocumentAdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobTypeDocuments>> GetAllAsync(
        string? jobTypeId,
        CancellationToken ct = default
    )
    {
        var query = _db
            .JobTypeDocuments.Include(x => x.JobType)
            .Include(x => x.DocumentType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobTypeId))
        {
            query = query.Where(x => x.JobTypeId == jobTypeId);
        }

        return await query
            .OrderBy(x => x.JobType.TypeName)
            .ThenBy(x => x.Sequence)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<JobTypeDocuments?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct).AsTask();
    }

    public async Task<bool> ExistsAsync(
        string jobTypeId,
        string documentTypeId,
        ProcurementCategory? procurementCategory,
        string? excludeId,
        CancellationToken ct = default
    )
    {
        var query = _db.JobTypeDocuments.AsQueryable();
        query = query.Where(x => x.JobTypeId == jobTypeId && x.DocumentTypeId == documentTypeId);
        if (procurementCategory.HasValue)
        {
            query = query.Where(x => x.ProcurementCategory == procurementCategory);
        }
        else
        {
            query = query.Where(x => x.ProcurementCategory == null);
        }
        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            query = query.Where(x => x.JobTypeDocumentId != excludeId);
        }

        return await query.AnyAsync(ct);
    }

    public async Task CreateAsync(JobTypeDocuments model, CancellationToken ct = default)
    {
        _db.JobTypeDocuments.Add(model);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobTypeDocuments model, CancellationToken ct = default)
    {
        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct);
        if (entity is null)
            return;

        _db.JobTypeDocuments.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<JobTypes>> GetJobTypesAsync(CancellationToken ct = default)
    {
        return await _db.JobTypes.AsNoTracking().OrderBy(x => x.TypeName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentType>> GetDocumentTypesAsync(
        CancellationToken ct = default
    )
    {
        return await _db.DocumentTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    }
}
