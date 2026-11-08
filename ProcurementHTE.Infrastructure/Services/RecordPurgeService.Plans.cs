using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Services;

public sealed partial class RecordPurgeService
{
    private static readonly HashSet<Type> ProcurementChildren =
    [
        typeof(ProcOffer),
        typeof(ProcDetail),
        typeof(ProcDocuments),
        typeof(ProfitLoss),
        typeof(ProfitLossSelectedVendor),
        typeof(VendorOffer),
        typeof(ProcurementStatusHistory),
    ];

    private Task<PurgePlan?> BuildPlanAsync(DeletedRecordKind kind, string id, CancellationToken ct) =>
        kind switch
        {
            DeletedRecordKind.Procurement => PlanProcurementAsync(id, ct),
            DeletedRecordKind.PurchaseRequisition => PlanPurchaseRequisitionAsync(id, ct),
            DeletedRecordKind.Vendor => PlanVendorAsync(id, ct),
            DeletedRecordKind.JobType => PlanJobTypeAsync(id, ct),
            DeletedRecordKind.DocumentType => PlanDocumentTypeAsync(id, ct),
            DeletedRecordKind.JobTypeDocument => PlanJobTypeDocumentAsync(id, ct),
            DeletedRecordKind.DocumentApproval => PlanDocumentApprovalAsync(id, ct),
            DeletedRecordKind.DocumentApprovalRule => PlanRuleAsync(id, ct),
            DeletedRecordKind.User => PlanUserAsync(id, ct),
            DeletedRecordKind.LdpArchive => PlanArchiveRowAsync(id, ct),
            _ => Task.FromResult<PurgePlan?>(null),
        };

    // Children go by name, not by cascade: ProfitLossItems, VendorOffers, and selected vendors
    // refer to ProfitLosses with NO ACTION, so a cascade from the job alone would fail.
    private async Task<PurgePlan?> PlanProcurementAsync(string id, CancellationToken ct)
    {
        var job = await Deleted<Procurement>().FirstOrDefaultAsync(x => x.ProcurementId == id, ct);
        if (job is null)
            return null;

        var offers = await All<ProcOffer>().Where(x => x.ProcurementId == id).ToListAsync(ct);
        var profitLosses = await All<ProfitLoss>().Where(x => x.ProcurementId == id).ToListAsync(ct);
        var documents = await All<ProcDocuments>().Where(x => x.ProcurementId == id).ToListAsync(ct);
        var offerIds = offers.Select(x => x.ProcOfferId).ToList();
        var profitLossIds = profitLosses.Select(x => x.ProfitLossId).ToList();
        var documentIds = documents.Select(x => x.ProcDocumentId).ToList();

        var rows = new List<object>();
        rows.AddRange(await All<VendorOffer>()
            .Where(x => x.ProcurementId == id || offerIds.Contains(x.ProcOfferId) || profitLossIds.Contains(x.ProfitLossId))
            .ToListAsync(ct));
        rows.AddRange(await All<ProfitLossItem>()
            .Where(x => offerIds.Contains(x.ProcOfferId) || profitLossIds.Contains(x.ProfitLossId))
            .ToListAsync(ct));
        rows.AddRange(await All<ProfitLossSelectedVendor>().Where(x => x.ProcurementId == id).ToListAsync(ct));
        rows.AddRange(await All<VendorRoundLetter>()
            .Where(x => x.ProcurementId == id || documentIds.Contains(x.ProcDocumentId))
            .ToListAsync(ct));
        rows.AddRange(await All<ProcDetail>().Where(x => x.ProcurementId == id).ToListAsync(ct));
        rows.AddRange(await All<ProcurementStatusHistory>().Where(x => x.ProcurementId == id).ToListAsync(ct));
        rows.AddRange(profitLosses);
        rows.AddRange(offers);
        rows.AddRange(documents);
        var relatedRows = rows.Count;
        rows.Add(job);

        var fileKeys = await UnsharedFilesAsync(
            documents
                .Select(x => (string?)x.ObjectKey)
                .Append(job.IspaFileObjectKey)
                .Append(job.LdpFileObjectKey)
                .Append(job.HardcopyEvidenceFilePath),
            procurementId: id,
            ct: ct
        );

        return new PurgePlan(
            job.ProcNum ?? "(tanpa nomor)",
            job.JobName,
            rows,
            relatedRows,
            fileKeys,
            await BlockersAsync<Procurement>(id, fk => ProcurementChildren.Contains(fk.DeclaringEntityType.ClrType), ct)
        );
    }

    private async Task<PurgePlan?> PlanPurchaseRequisitionAsync(string id, CancellationToken ct)
    {
        var pr = await Deleted<PurchaseRequisition>().FirstOrDefaultAsync(x => x.PrId == id, ct);
        if (pr is null)
            return null;

        var rows = new List<object>(await All<PurchaseRequisitionStatusHistory>().Where(x => x.PrId == id).ToListAsync(ct));
        var relatedRows = rows.Count;
        rows.Add(pr);

        return new PurgePlan(
            pr.PrNumber ?? "(tanpa nomor)",
            pr.Description,
            rows,
            relatedRows,
            await UnsharedFilesAsync([pr.DocumentFilePath, pr.HardcopyEvidenceFilePath], prId: id, ct: ct),
            await BlockersAsync<PurchaseRequisition>(
                id,
                fk => fk.DeclaringEntityType.ClrType == typeof(PurchaseRequisitionStatusHistory),
                ct
            )
        );
    }

    private async Task<PurgePlan?> PlanVendorAsync(string id, CancellationToken ct)
    {
        var vendor = await Deleted<Vendor>().FirstOrDefaultAsync(x => x.VendorId == id, ct);
        return vendor is null
            ? null
            : new PurgePlan(vendor.VendorName, vendor.VendorCode, [vendor], 0, [], await BlockersAsync<Vendor>(id, _ => false, ct));
    }

    private async Task<PurgePlan?> PlanJobTypeAsync(string id, CancellationToken ct)
    {
        var jobType = await Deleted<JobTypes>().FirstOrDefaultAsync(x => x.JobTypeId == id, ct);
        if (jobType is null)
            return null;

        var rows = await DocumentMappingRowsAsync(All<JobTypeDocuments>().Where(x => x.JobTypeId == id), ct);
        var relatedRows = rows.Count;
        rows.Add(jobType);

        return new PurgePlan(
            jobType.TypeName,
            jobType.Description,
            rows,
            relatedRows,
            [],
            await BlockersAsync<JobTypes>(id, fk => fk.DeclaringEntityType.ClrType == typeof(JobTypeDocuments), ct)
        );
    }

    private async Task<PurgePlan?> PlanDocumentTypeAsync(string id, CancellationToken ct)
    {
        var documentType = await Deleted<DocumentType>().FirstOrDefaultAsync(x => x.DocumentTypeId == id, ct);
        if (documentType is null)
            return null;

        var rows = await DocumentMappingRowsAsync(All<JobTypeDocuments>().Where(x => x.DocumentTypeId == id), ct);
        rows.AddRange(await All<DocumentApprovalRule>().Where(x => x.DocumentTypeId == id).ToListAsync(ct));
        var relatedRows = rows.Count;
        rows.Add(documentType);

        return new PurgePlan(
            documentType.Name,
            documentType.Description,
            rows,
            relatedRows,
            [],
            await BlockersAsync<DocumentType>(
                id,
                fk => fk.DeclaringEntityType.ClrType == typeof(JobTypeDocuments)
                    || fk.DeclaringEntityType.ClrType == typeof(DocumentApprovalRule),
                ct
            )
        );
    }

    private async Task<PurgePlan?> PlanJobTypeDocumentAsync(string id, CancellationToken ct)
    {
        var mapping = await Deleted<JobTypeDocuments>()
            .Include(x => x.DocumentType)
            .Include(x => x.JobType)
            .FirstOrDefaultAsync(x => x.JobTypeDocumentId == id, ct);
        if (mapping is null)
            return null;

        var rows = await DocumentMappingRowsAsync(All<JobTypeDocuments>().Where(x => x.JobTypeDocumentId == id), ct);

        return new PurgePlan(
            mapping.DocumentType?.Name ?? id,
            $"Procurement type: {mapping.JobType?.TypeName}",
            rows,
            rows.Count - 1,
            [],
            await BlockersAsync<JobTypeDocuments>(id, fk => fk.DeclaringEntityType.ClrType == typeof(DocumentApprovals), ct)
        );
    }

    private async Task<PurgePlan?> PlanDocumentApprovalAsync(string id, CancellationToken ct)
    {
        var approval = await Deleted<DocumentApprovals>()
            .Include(x => x.JobTypeDocument).ThenInclude(x => x.DocumentType)
            .Include(x => x.JobTypeDocument).ThenInclude(x => x.JobType)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.DocumentApprovalId == id, ct);

        return approval is null
            ? null
            : new PurgePlan(
                $"{approval.JobTypeDocument?.DocumentType?.Name} · level {approval.Level}",
                $"{approval.JobTypeDocument?.JobType?.TypeName} · {approval.Role?.Name}",
                [approval],
                0,
                [],
                []
            );
    }

    private async Task<PurgePlan?> PlanRuleAsync(string id, CancellationToken ct)
    {
        var rule = await Deleted<DocumentApprovalRule>()
            .Include(x => x.DocumentType)
            .FirstOrDefaultAsync(x => x.DocumentApprovalRuleId == id, ct);

        return rule is null
            ? null
            : new PurgePlan(rule.DocumentType?.Name ?? id, $"{rule.MinAmount:N0} – {rule.MaxAmount:N0}", [rule], 0, [], []);
    }

    // Claims, roles, sessions, and personal notifications cascade with the account. Any other
    // reference is work the person did, so that account stays in Deleted Records.
    private async Task<PurgePlan?> PlanUserAsync(string id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, ct);
        if (user is null)
            return null;

        var rows = new List<object>(await All<RefreshToken>().Where(x => x.UserId == id).ToListAsync(ct));
        var relatedRows = rows.Count;
        foreach (var foreignKey in db.Model.FindEntityType(typeof(User))!.GetReferencingForeignKeys())
        {
            if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade)
                relatedRows += await CountReferencesAsync(foreignKey, id, ct);
        }
        rows.Add(user);

        return new PurgePlan(
            user.FullName ?? user.UserName ?? id,
            user.Email,
            rows,
            relatedRows,
            await UnsharedFilesAsync([user.AvatarObjectKey], userId: id, ct: ct),
            await BlockersAsync<User>(id, fk => fk.DeleteBehavior == DeleteBehavior.Cascade, ct)
        );
    }

    private async Task<PurgePlan?> PlanArchiveRowAsync(string id, CancellationToken ct)
    {
        var row = await Deleted<LdpArchiveRow>().FirstOrDefaultAsync(x => x.LdpArchiveRowId == id, ct);
        return row is null
            ? null
            : new PurgePlan(row.JobName ?? "(tanpa nama pekerjaan)", $"{row.VendorName} · PR {row.NoPr}", [row], 0, [], []);
    }

    private async Task<List<object>> DocumentMappingRowsAsync(IQueryable<JobTypeDocuments> query, CancellationToken ct)
    {
        var mappings = await query.ToListAsync(ct);
        var mappingIds = mappings.Select(x => x.JobTypeDocumentId).ToList();

        var rows = new List<object>(await All<DocumentApprovals>()
            .Where(x => mappingIds.Contains(x.JobTypeDocumentId))
            .ToListAsync(ct));
        rows.AddRange(mappings);
        return rows;
    }

    // One INPRO file can serve several jobs, and a soft-deleted record can still be restored,
    // so a file stays until no other row anywhere points at it.
    private async Task<IReadOnlyList<string>> UnsharedFilesAsync(
        IEnumerable<string?> keys,
        string? procurementId = null,
        string? prId = null,
        string? userId = null,
        CancellationToken ct = default
    )
    {
        var unshared = new List<string>();
        foreach (var key in keys.OfType<string>().Where(key => key.Length > 0).Distinct())
        {
            var shared =
                await All<ProcDocuments>().AnyAsync(x => x.ObjectKey == key && x.ProcurementId != procurementId, ct)
                || await All<Procurement>().AnyAsync(
                    x => x.ProcurementId != procurementId
                        && (x.IspaFileObjectKey == key || x.LdpFileObjectKey == key || x.HardcopyEvidenceFilePath == key),
                    ct
                )
                || await All<PurchaseRequisition>().AnyAsync(
                    x => x.PrId != prId && (x.DocumentFilePath == key || x.HardcopyEvidenceFilePath == key),
                    ct
                )
                || await db.Users.AnyAsync(x => x.Id != userId && x.AvatarObjectKey == key, ct);

            if (!shared)
                unshared.Add(key);
        }

        return unshared;
    }
}
