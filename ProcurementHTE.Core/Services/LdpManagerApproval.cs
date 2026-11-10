namespace ProcurementHTE.Core.Services;

/// <summary>
/// The manager's approval of a spreadsheet job that shows in the LDP. Such a job already has a PO,
/// so it passed the approval, dated no later than <see cref="MaxDaysAfterStart"/> days after the
/// job started. This applies to imported spreadsheet rows only: a job run in Docutrax keeps the
/// date its approver signed.
/// </summary>
public static class LdpManagerApproval
{
    public const int MaxDaysAfterStart = 3;

    /// <summary>The finish date: the recorded one when it is on time, otherwise the latest date allowed.</summary>
    public static DateTime Finished(DateTime start, DateTime? recorded)
    {
        var latest = start.Date.AddDays(MaxDaysAfterStart);
        return recorded is { } date && date.Date <= latest ? date : latest;
    }

    /// <summary>The start date, moved back to <paramref name="finished"/> when it fell after it.</summary>
    public static DateTime? Started(DateTime? recorded, DateTime finished) =>
        recorded is { } date && date > finished ? finished : recorded;
}
