using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>Permanently deletes soft-deleted records and keeps a log of every delete.</summary>
public interface IRecordPurgeService
{
    /// <summary>What a delete would remove, or null when the record is not in Deleted Records.</summary>
    Task<PurgePreview?> PreviewAsync(DeletedRecordKind kind, string id, CancellationToken ct = default);

    Task<PurgeResult> PurgeAsync(PurgeRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PermanentDeletionLog>> ListLogAsync(CancellationToken ct = default);
}
