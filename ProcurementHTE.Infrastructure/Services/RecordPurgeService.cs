using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Services;

public sealed partial class RecordPurgeService(
    AppDbContext db,
    IObjectStorage storage,
    IOptions<ObjectStorageOptions> storageOptions,
    ILogger<RecordPurgeService> logger
) : IRecordPurgeService
{
    private const int LogListLimit = 500;
    private const string NotFound = "Data tidak ditemukan, atau belum ada di Data Terhapus.";

    private static readonly MethodInfo CountWhereMethod = typeof(RecordPurgeService).GetMethod(
        nameof(CountWhereAsync),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    private sealed record PurgePlan(
        string Title,
        string? Detail,
        IReadOnlyList<object> Rows,
        int RelatedRows,
        IReadOnlyList<string> FileKeys,
        IReadOnlyList<string> Blockers
    );

    public async Task<PurgePreview?> PreviewAsync(DeletedRecordKind kind, string id, CancellationToken ct = default)
    {
        var plan = await BuildPlanAsync(kind, id, ct);
        return plan is null
            ? null
            : new PurgePreview(kind, id, plan.Title, plan.Detail, plan.RelatedRows, plan.FileKeys.Count, plan.Blockers);
    }

    public async Task<PurgeResult> PurgeAsync(PurgeRequest request, CancellationToken ct = default)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < PurgeRequest.MinReasonLength)
            return PurgeResult.Fail($"Alasan minimal {PurgeRequest.MinReasonLength} karakter.");

        var plan = await BuildPlanAsync(request.Kind, request.Id, ct);
        if (plan is null)
            return PurgeResult.Fail(NotFound);
        if (plan.Blockers.Count > 0)
            return PurgeResult.Fail($"Data masih dipakai: {string.Join(", ", plan.Blockers)}.");

        var actorName = await db.Users
            .Where(user => user.Id == request.ActorUserId)
            .Select(user => user.FullName ?? user.UserName)
            .FirstOrDefaultAsync(ct);

        var log = NewLog(request, actorName, plan.Title, plan.Detail, reason);
        log.RelatedRows = plan.RelatedRows;
        log.FileCount = plan.FileKeys.Count;

        // One SaveChanges means one transaction: no delete without its log, and no log without its delete.
        db.PermanentDeletionLogs.Add(log);
        db.RemoveRange(plan.Rows);
        db.PermanentDeleteAllowed = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Permanent delete of {Kind} {Id} failed.", request.Kind, request.Id);
            db.ChangeTracker.Clear();
            return PurgeResult.Fail("Data masih dipakai data lain, jadi tidak ada yang dihapus.");
        }
        finally
        {
            db.PermanentDeleteAllowed = false;
        }

        var failedKeys = await DeleteFilesAsync(plan.FileKeys);
        if (failedKeys.Count == 0)
            return PurgeResult.Ok($"{plan.Title} dihapus permanen.");

        // The log is append-only, so a failed file gets a row of its own.
        var followUp = NewLog(
            request,
            actorName,
            plan.Title,
            string.Join(", ", failedKeys),
            "File gagal dihapus dari penyimpanan dan perlu dibersihkan manual."
        );
        followUp.FilesFailed = failedKeys.Count;
        db.PermanentDeletionLogs.Add(followUp);
        await db.SaveChangesAsync(CancellationToken.None);

        return PurgeResult.Ok($"{plan.Title} dihapus permanen, tapi {failedKeys.Count} file gagal dihapus dan sudah dicatat di log.");
    }

    public async Task<IReadOnlyList<PermanentDeletionLog>> ListLogAsync(CancellationToken ct = default) =>
        await db.PermanentDeletionLogs
            .AsNoTracking()
            .OrderByDescending(log => log.DeletedAt)
            .Take(LogListLimit)
            .ToListAsync(ct);

    private static PermanentDeletionLog NewLog(
        PurgeRequest request,
        string? actorName,
        string title,
        string? detail,
        string reason
    ) =>
        new()
        {
            Kind = request.Kind.ToString(),
            RecordId = request.Id,
            Title = Clip(title, 300)!,
            Detail = Clip(detail, 500),
            Reason = Clip(reason, 500)!,
            DeletedByUserId = request.ActorUserId,
            DeletedByName = Clip(actorName ?? request.ActorUserId, 256)!,
            IpAddress = Clip(request.IpAddress, 64),
        };

    private static string? Clip(string? value, int length) =>
        value is { } text && text.Length > length ? text[..length] : value;

    // The rows are already gone, so a closed browser tab must not stop this.
    private async Task<IReadOnlyList<string>> DeleteFilesAsync(IReadOnlyList<string> keys)
    {
        var failed = new List<string>();
        foreach (var key in keys)
        {
            try
            {
                await storage.DeleteAsync(storageOptions.Value.Bucket, key, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not delete file {ObjectKey} after a permanent delete.", key);
                failed.Add(key);
            }
        }

        return failed;
    }

    // Reads every foreign key from the EF model, so a new relationship is checked without
    // touching this code. A cascade counts too: it would otherwise take live data with it.
    private async Task<IReadOnlyList<string>> BlockersAsync<T>(
        string id,
        Func<IForeignKey, bool> belongsToRecord,
        CancellationToken ct
    )
        where T : class
    {
        var references = new Dictionary<string, int>();
        foreach (var foreignKey in db.Model.FindEntityType(typeof(T))!.GetReferencingForeignKeys())
        {
            if (belongsToRecord(foreignKey))
                continue;

            var count = await CountReferencesAsync(foreignKey, id, ct);
            if (count == 0)
                continue;

            var label = Label(foreignKey.DeclaringEntityType.ClrType);
            references[label] = references.GetValueOrDefault(label) + count;
        }

        return references.Select(reference => $"{reference.Key} ({reference.Value})").ToList();
    }

    private Task<int> CountReferencesAsync(IForeignKey foreignKey, string id, CancellationToken ct) =>
        (Task<int>)CountWhereMethod
            .MakeGenericMethod(foreignKey.DeclaringEntityType.ClrType)
            .Invoke(this, [foreignKey.Properties[0].Name, id, ct])!;

    private Task<int> CountWhereAsync<T>(string property, string id, CancellationToken ct)
        where T : class =>
        db.Set<T>().IgnoreQueryFilters().CountAsync(row => EF.Property<string>(row, property) == id, ct);

    private static string Label(Type type) =>
        type.Name switch
        {
            nameof(Procurement) => "Procurement",
            nameof(PurchaseRequisition) => "PR",
            nameof(ProcDocuments) => "Dokumen procurement",
            nameof(ProcDetail) => "Detail procurement",
            nameof(ProfitLoss) => "Profit & Loss",
            nameof(ProfitLossSelectedVendor) => "Vendor terpilih",
            nameof(VendorOffer) => "Penawaran vendor",
            nameof(VendorRoundLetter) => "Surat ronde vendor",
            nameof(DocumentApprovalRule) => "Aturan approval",
            nameof(ProcurementStatusHistory) => "Riwayat status procurement",
            nameof(PurchaseRequisitionStatusHistory) => "Riwayat status PR",
            nameof(Notification) => "Notifikasi",
            _ => type.Name,
        };

    private IQueryable<T> All<T>()
        where T : class => db.Set<T>().IgnoreQueryFilters();

    private IQueryable<T> Deleted<T>()
        where T : class, ISoftDeletable => All<T>().Where(row => row.IsDeleted);
}
