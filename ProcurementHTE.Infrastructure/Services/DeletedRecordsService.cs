using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Services;

public sealed partial class DeletedRecordsService(AppDbContext db) : IDeletedRecordsService
{
    private const int ListLimit = 500;
    private const string NotFound = "Data tidak ditemukan atau sudah dipulihkan.";

    public async Task<IReadOnlyDictionary<DeletedRecordKind, int>> CountAsync(CancellationToken ct = default) =>
        new Dictionary<DeletedRecordKind, int>
        {
            [DeletedRecordKind.Procurement] = await Deleted<Procurement>().CountAsync(ct),
            [DeletedRecordKind.PurchaseRequisition] = await Deleted<PurchaseRequisition>().CountAsync(ct),
            [DeletedRecordKind.Vendor] = await Deleted<Vendor>().CountAsync(ct),
            [DeletedRecordKind.JobType] = await Deleted<JobTypes>().CountAsync(ct),
            [DeletedRecordKind.DocumentType] = await Deleted<DocumentType>().CountAsync(ct),
            [DeletedRecordKind.JobTypeDocument] = await Deleted<JobTypeDocuments>().CountAsync(ct),
            [DeletedRecordKind.DocumentApproval] = await Deleted<DocumentApprovals>().CountAsync(ct),
            [DeletedRecordKind.DocumentApprovalRule] = await Deleted<DocumentApprovalRule>().CountAsync(ct),
            [DeletedRecordKind.User] = await db.Users.CountAsync(u => u.IsDeleted, ct),
        };

    public async Task<IReadOnlyList<DeletedRecordItem>> ListAsync(DeletedRecordKind kind, CancellationToken ct = default)
    {
        var rows = kind switch
        {
            DeletedRecordKind.Procurement => await Deleted<Procurement>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.ProcurementId, x.ProcNum ?? "(tanpa nomor)", x.JobName, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.PurchaseRequisition => await Deleted<PurchaseRequisition>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.PrId, x.PrNumber, x.Description, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.Vendor => await Deleted<Vendor>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.VendorId, x.VendorName, x.VendorCode, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.JobType => await Deleted<JobTypes>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.JobTypeId, x.TypeName, x.Description, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.DocumentType => await Deleted<DocumentType>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.DocumentTypeId, x.Name, x.Description, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.JobTypeDocument => await Deleted<JobTypeDocuments>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(
                    x.JobTypeDocumentId,
                    x.DocumentType.Name,
                    "Procurement type: " + x.JobType.TypeName,
                    x.DeletedAt,
                    x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.DocumentApproval => await Deleted<DocumentApprovals>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(
                    x.DocumentApprovalId,
                    x.JobTypeDocument.DocumentType.Name + " · level " + x.Level,
                    x.JobTypeDocument.JobType.TypeName + " · " + x.Role.Name,
                    x.DeletedAt,
                    x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.DocumentApprovalRule => await Deleted<DocumentApprovalRule>()
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(
                    x.DocumentApprovalRuleId,
                    x.DocumentType.Name,
                    (x.JobType != null ? x.JobType.TypeName : "Semua procurement type")
                        + " · " + x.MinAmount.ToString() + " – " + x.MaxAmount.ToString(),
                    x.DeletedAt,
                    x.DeletedBy))
                .ToListAsync(ct),
            DeletedRecordKind.User => await db.Users
                .Where(u => u.IsDeleted)
                .OrderByDescending(x => x.DeletedAt).Take(ListLimit)
                .Select(x => new Row(x.Id, x.FullName ?? x.UserName ?? x.Id, x.Email, x.DeletedAt, x.DeletedBy))
                .ToListAsync(ct),
            _ => [],
        };

        var deleterIds = rows.Where(r => r.DeletedBy != null).Select(r => r.DeletedBy!).Distinct().ToList();
        var deleters = await db.Users
            .Where(u => deleterIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.UserName })
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return rows
            .Select(r => new DeletedRecordItem(
                r.Id,
                kind switch
                {
                    DeletedRecordKind.Procurement => StripLegacyDash(r.Title)!,
                    DeletedRecordKind.PurchaseRequisition => StripLegacyPrNumber(r.Title),
                    _ => r.Title,
                },
                kind == DeletedRecordKind.Vendor ? StripLegacyDash(r.Detail) : r.Detail,
                r.DeletedAt ?? DateTime.MinValue,
                r.DeletedBy != null && deleters.TryGetValue(r.DeletedBy, out var name) ? name : null))
            .ToList();
    }

    public async Task<RestoreResult> RestoreAsync(DeletedRecordKind kind, string id, CancellationToken ct = default)
    {
        var result = kind switch
        {
            DeletedRecordKind.Procurement => await RestoreProcurementAsync(id, ct),
            DeletedRecordKind.PurchaseRequisition => await RestorePurchaseRequisitionAsync(id, ct),
            DeletedRecordKind.Vendor => await RestoreVendorAsync(id, ct),
            DeletedRecordKind.JobType => await RestoreJobTypeAsync(id, ct),
            DeletedRecordKind.DocumentType => await RestoreDocumentTypeAsync(id, ct),
            DeletedRecordKind.JobTypeDocument => await RestoreJobTypeDocumentAsync(id, ct),
            DeletedRecordKind.DocumentApproval => await RestoreDocumentApprovalAsync(id, ct),
            DeletedRecordKind.DocumentApprovalRule => await RestoreRuleAsync(id, ct),
            DeletedRecordKind.User => await RestoreUserAsync(id, ct),
            _ => RestoreResult.Fail("Jenis data tidak dikenal."),
        };

        if (result.Succeeded)
            await db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<RestoreResult> RestoreProcurementAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<Procurement>().FirstOrDefaultAsync(x => x.ProcurementId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);

        if (item.JobTypeId != null && await Deleted<JobTypes>().AnyAsync(j => j.JobTypeId == item.JobTypeId, ct))
            return RestoreResult.Fail("Procurement type-nya masih terhapus. Pulihkan procurement type itu dulu.");

        var number = StripLegacyDash(item.ProcNum);
        if (number != null && await db.Procurements.AnyAsync(p => p.ProcNum == number, ct))
            return RestoreResult.Fail($"Nomor {number} sudah dipakai procurement lain yang aktif.");

        item.ProcNum = number;
        item.IsDeleted = false;
        return RestoreResult.Ok($"Procurement {number ?? item.JobName} dipulihkan.");
    }

    private async Task<RestoreResult> RestorePurchaseRequisitionAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<PurchaseRequisition>().FirstOrDefaultAsync(x => x.PrId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);

        var number = StripLegacyPrNumber(item.PrNumber);
        if (await db.PurchaseRequisitions.AnyAsync(p => p.PrNumber == number, ct))
            return RestoreResult.Fail($"Nomor PR {number} sudah dipakai PR lain yang aktif.");

        item.PrNumber = number;
        item.IsDeleted = false;
        return RestoreResult.Ok($"PR {number} dipulihkan.");
    }

    private async Task<RestoreResult> RestoreVendorAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<Vendor>().FirstOrDefaultAsync(x => x.VendorId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);

        var code = StripLegacyDash(item.VendorCode)!;
        if (await db.Vendors.AnyAsync(v => v.VendorCode == code, ct))
            return RestoreResult.Fail($"Kode vendor {code} sudah dipakai vendor lain yang aktif.");

        item.VendorCode = code;
        item.IsDeleted = false;
        return RestoreResult.Ok($"Vendor {item.VendorName} dipulihkan.");
    }

    private async Task<RestoreResult> RestoreJobTypeAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<JobTypes>().FirstOrDefaultAsync(x => x.JobTypeId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);

        if (await db.JobTypes.AnyAsync(j => j.TypeName == item.TypeName, ct))
            return RestoreResult.Fail($"Sudah ada procurement type aktif bernama \"{item.TypeName}\".");

        var at = item.DeletedAt;
        item.IsDeleted = false;
        // Bring back the setup deleted together with it, unless its document type is still deleted.
        var documents = await Deleted<JobTypeDocuments>()
            .Where(d => d.JobTypeId == id && d.DeletedAt == at && !d.DocumentType.IsDeleted)
            .ToListAsync(ct);
        await RestoreJobTypeDocumentsAsync(documents, at, ct);
        foreach (var rule in await Deleted<DocumentApprovalRule>()
            .Where(r => r.JobTypeId == id && r.DeletedAt == at && !r.DocumentType.IsDeleted)
            .ToListAsync(ct))
            rule.IsDeleted = false;

        return RestoreResult.Ok($"Procurement type {item.TypeName} dipulihkan beserta konfigurasi dokumennya.");
    }

    private async Task<RestoreResult> RestoreDocumentTypeAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<DocumentType>().FirstOrDefaultAsync(x => x.DocumentTypeId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);

        if (await db.DocumentTypes.AnyAsync(d => d.Name == item.Name, ct))
            return RestoreResult.Fail($"Sudah ada document type aktif bernama \"{item.Name}\".");

        var at = item.DeletedAt;
        item.IsDeleted = false;
        var documents = await Deleted<JobTypeDocuments>()
            .Where(d => d.DocumentTypeId == id && d.DeletedAt == at && !d.JobType.IsDeleted)
            .ToListAsync(ct);
        await RestoreJobTypeDocumentsAsync(documents, at, ct);
        foreach (var rule in await Deleted<DocumentApprovalRule>()
            .Where(r => r.DocumentTypeId == id && r.DeletedAt == at && (r.JobType == null || !r.JobType.IsDeleted))
            .ToListAsync(ct))
            rule.IsDeleted = false;

        return RestoreResult.Ok($"Document type {item.Name} dipulihkan beserta konfigurasinya.");
    }

    private async Task<RestoreResult> RestoreJobTypeDocumentAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<JobTypeDocuments>()
            .Include(d => d.JobType)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(x => x.JobTypeDocumentId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);
        if (item.JobType.IsDeleted)
            return RestoreResult.Fail($"Procurement type \"{item.JobType.TypeName}\" masih terhapus. Pulihkan itu dulu.");
        if (item.DocumentType.IsDeleted)
            return RestoreResult.Fail($"Document type \"{item.DocumentType.Name}\" masih terhapus. Pulihkan itu dulu.");
        if (await db.JobTypeDocuments.AnyAsync(d => d.JobTypeId == item.JobTypeId && d.DocumentTypeId == item.DocumentTypeId, ct))
            return RestoreResult.Fail("Dokumen ini sudah dikonfigurasi lagi untuk procurement type yang sama.");

        await RestoreJobTypeDocumentsAsync([item], item.DeletedAt, ct);
        return RestoreResult.Ok($"Konfigurasi dokumen {item.DocumentType.Name} dipulihkan.");
    }

    private async Task<RestoreResult> RestoreDocumentApprovalAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<DocumentApprovals>().FirstOrDefaultAsync(x => x.DocumentApprovalId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);
        if (!await db.JobTypeDocuments.AnyAsync(d => d.JobTypeDocumentId == item.JobTypeDocumentId, ct))
            return RestoreResult.Fail("Konfigurasi dokumennya masih terhapus. Pulihkan itu dulu.");

        item.IsDeleted = false;
        return RestoreResult.Ok("Approval dokumen dipulihkan.");
    }

    private async Task<RestoreResult> RestoreRuleAsync(string id, CancellationToken ct)
    {
        var item = await Deleted<DocumentApprovalRule>().FirstOrDefaultAsync(x => x.DocumentApprovalRuleId == id, ct);
        if (item is null)
            return RestoreResult.Fail(NotFound);
        if (!await db.DocumentTypes.AnyAsync(d => d.DocumentTypeId == item.DocumentTypeId, ct))
            return RestoreResult.Fail("Document type-nya masih terhapus. Pulihkan itu dulu.");
        if (item.JobTypeId != null && !await db.JobTypes.AnyAsync(j => j.JobTypeId == item.JobTypeId, ct))
            return RestoreResult.Fail("Procurement type-nya masih terhapus. Pulihkan itu dulu.");

        item.IsDeleted = false;
        return RestoreResult.Ok("Aturan approval dipulihkan.");
    }

    private async Task<RestoreResult> RestoreUserAsync(string id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted, ct);
        if (user is null)
            return RestoreResult.Fail(NotFound);

        user.IsDeleted = false;
        user.IsActive = true;
        return RestoreResult.Ok($"User {user.UserName} dipulihkan dan bisa login lagi.");
    }

    private async Task RestoreJobTypeDocumentsAsync(IReadOnlyList<JobTypeDocuments> documents, DateTime? at, CancellationToken ct)
    {
        if (documents.Count == 0)
            return;

        var ids = documents.Select(d => d.JobTypeDocumentId).ToList();
        foreach (var document in documents)
            document.IsDeleted = false;
        foreach (var approval in await Deleted<DocumentApprovals>()
            .Where(a => ids.Contains(a.JobTypeDocumentId) && a.DeletedAt == at)
            .ToListAsync(ct))
            approval.IsDeleted = false;
    }

    private IQueryable<T> Deleted<T>() where T : class, ISoftDeletable =>
        db.Set<T>().IgnoreQueryFilters().Where(x => x.IsDeleted);

    // Older deletes renamed numbers so they would not collide; undo that on restore.
    internal static string? StripLegacyDash(string? value) =>
        value is { Length: > 1 } && value[0] == '-' ? value[1..] : value;

    internal static string StripLegacyPrNumber(string value)
    {
        var match = LegacyPrNumber().Match(value);
        return match.Success ? match.Groups[1].Value : value;
    }

    [GeneratedRegex(@"^DELETED_(.+)_\d{17}$")]
    private static partial Regex LegacyPrNumber();

    private sealed record Row(string Id, string Title, string? Detail, DateTime? DeletedAt, string? DeletedBy);
}
