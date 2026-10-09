using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Exceptions;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

/// <summary>
/// Soft deletes master data together with the configuration that hangs off it.
/// Every row in one deletion shares the same DeletedAt, which is how a restore
/// finds and brings back exactly that group. Callers save the context.
/// </summary>
public static class MasterDataDeletion
{
    public static async Task DeleteJobTypeAsync(AppDbContext db, string jobTypeId, CancellationToken ct = default)
    {
        var jobType = await db.JobTypes.FirstOrDefaultAsync(j => j.JobTypeId == jobTypeId, ct);
        if (jobType is null)
            return;

        var inUse = await db.Procurements.CountAsync(p => p.JobTypeId == jobTypeId, ct);
        if (inUse > 0)
            throw new EntityInUseException(
                $"Procurement type \"{jobType.TypeName}\" masih dipakai {inUse} procurement aktif, jadi tidak bisa dihapus."
            );

        var at = DateTime.UtcNow;
        Mark(jobType, at);
        await MarkJobTypeDocumentsAsync(db, db.JobTypeDocuments.Where(d => d.JobTypeId == jobTypeId), at, ct);
        foreach (var rule in await db.DocumentApprovalRules.Where(r => r.JobTypeId == jobTypeId).ToListAsync(ct))
            Mark(rule, at);
    }

    public static async Task DeleteDocumentTypeAsync(AppDbContext db, string documentTypeId, CancellationToken ct = default)
    {
        var documentType = await db.DocumentTypes.FirstOrDefaultAsync(d => d.DocumentTypeId == documentTypeId, ct);
        if (documentType is null)
            return;

        var inUse = await db.ProcDocuments.CountAsync(
            d => d.DocumentTypeId == documentTypeId && !d.Procurement.IsDeleted,
            ct
        );
        if (inUse > 0)
            throw new EntityInUseException(
                $"Document type \"{documentType.Name}\" masih dipakai {inUse} dokumen procurement aktif, jadi tidak bisa dihapus."
            );

        var at = DateTime.UtcNow;
        Mark(documentType, at);
        await MarkJobTypeDocumentsAsync(db, db.JobTypeDocuments.Where(d => d.DocumentTypeId == documentTypeId), at, ct);
        foreach (var rule in await db.DocumentApprovalRules.Where(r => r.DocumentTypeId == documentTypeId).ToListAsync(ct))
            Mark(rule, at);
    }

    public static Task DeleteJobTypeDocumentAsync(AppDbContext db, string jobTypeDocumentId, CancellationToken ct = default) =>
        MarkJobTypeDocumentsAsync(
            db,
            db.JobTypeDocuments.Where(d => d.JobTypeDocumentId == jobTypeDocumentId),
            DateTime.UtcNow,
            ct
        );

    private static async Task MarkJobTypeDocumentsAsync(
        AppDbContext db,
        IQueryable<JobTypeDocuments> query,
        DateTime at,
        CancellationToken ct
    )
    {
        var documents = await query.ToListAsync(ct);
        if (documents.Count == 0)
            return;

        var ids = documents.Select(d => d.JobTypeDocumentId).ToList();
        foreach (var document in documents)
            Mark(document, at);
        foreach (var approval in await db.DocumentApprovals.Where(a => ids.Contains(a.JobTypeDocumentId)).ToListAsync(ct))
            Mark(approval, at);
    }

    private static void Mark(ISoftDeletable entity, DateTime at)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = at;
    }
}
