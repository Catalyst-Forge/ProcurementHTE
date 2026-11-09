using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>Reads and changes the range of archived jobs shown in the LDP.</summary>
public interface ILdpArchiveRangeService
{
    Task<LdpArchiveRange> GetAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LdpArchiveRangeChange>> HistoryAsync(CancellationToken ct = default);

    Task<LdpArchiveRangeResult> ChangeAsync(DateTime? from, DateTime? to, string userId, CancellationToken ct = default);
}
