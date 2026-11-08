using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface ILdpArchiveRepository
{
    Task<Common.PagedResult<LdpArchiveListItem>> SearchAsync(LdpArchiveQuery query, CancellationToken ct = default);

    Task<LdpArchiveRow?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>Every row the query matches, in its order, with all spreadsheet values.</summary>
    Task<IReadOnlyList<LdpArchiveRow>> ExportAsync(LdpArchiveQuery query, CancellationToken ct = default);

    /// <summary>A tracked row for an edit, saved with <see cref="SaveAsync"/>.</summary>
    Task<LdpArchiveRow?> FindForEditAsync(string id, CancellationToken ct = default);

    void Add(LdpArchiveRow row);

    /// <summary>A soft delete: the row goes to Deleted Records.</summary>
    void Remove(LdpArchiveRow row);

    Task SaveAsync(CancellationToken ct = default);
}
