using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// Saves a new LDP range: refuses one that is not valid, records nothing when the range is
/// already in force, and otherwise records the change with the range it replaces.
/// </summary>
public sealed class LdpArchiveRangeService(ILdpArchiveRangeRepository ranges) : ILdpArchiveRangeService
{
    public Task<LdpArchiveRange> GetAsync(CancellationToken ct = default) => ranges.GetAsync(ct);

    public Task<IReadOnlyList<LdpArchiveRangeChange>> HistoryAsync(CancellationToken ct = default) => ranges.HistoryAsync(ct);

    public async Task<LdpArchiveRangeResult> ChangeAsync(DateTime? from, DateTime? to, string userId, CancellationToken ct = default)
    {
        if (LdpArchiveRange.Validate(from, to) is { } message)
            return new LdpArchiveRangeResult.Invalid(message);

        var range = new LdpArchiveRange(from!.Value, to!.Value);
        var previous = await ranges.GetAsync(ct);
        if (range == previous)
            return new LdpArchiveRangeResult.Unchanged();

        await ranges.AddAsync(
            new LdpArchiveRangeChange
            {
                StartFrom = range.From,
                StartTo = range.To,
                PreviousStartFrom = previous.From,
                PreviousStartTo = previous.To,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
            },
            ct
        );
        return new LdpArchiveRangeResult.Changed(range, range.Widens(previous));
    }
}
