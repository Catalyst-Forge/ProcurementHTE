using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>Lists soft-deleted records and restores them with integrity checks.</summary>
public interface IDeletedRecordsService
{
    Task<IReadOnlyDictionary<DeletedRecordKind, int>> CountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DeletedRecordItem>> ListAsync(DeletedRecordKind kind, CancellationToken ct = default);

    Task<RestoreResult> RestoreAsync(DeletedRecordKind kind, string id, CancellationToken ct = default);
}
