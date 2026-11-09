using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>What saving a new LDP range did.</summary>
public abstract record LdpArchiveRangeResult
{
    /// <summary>Not saved; the message says why.</summary>
    public sealed record Invalid(string Message) : LdpArchiveRangeResult;

    /// <summary>The same range was already in force, so nothing was recorded.</summary>
    public sealed record Unchanged : LdpArchiveRangeResult;

    /// <summary>Saved. <paramref name="Widened"/> when it covers days the old range did not.</summary>
    public sealed record Changed(LdpArchiveRange Range, bool Widened) : LdpArchiveRangeResult;
}
