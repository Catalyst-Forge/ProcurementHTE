using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>The range of archived jobs shown in the LDP, and its change history.</summary>
public interface ILdpArchiveRangeRepository
{
    /// <summary>The range in force: the newest change, or <see cref="LdpArchiveRange.Default"/>.</summary>
    Task<LdpArchiveRange> GetAsync(CancellationToken ct = default);

    /// <summary>Every change, newest first.</summary>
    Task<IReadOnlyList<LdpArchiveRangeChange>> HistoryAsync(CancellationToken ct = default);

    /// <summary>Records a change, filling in the name of the user who made it.</summary>
    Task AddAsync(LdpArchiveRangeChange change, CancellationToken ct = default);
}
