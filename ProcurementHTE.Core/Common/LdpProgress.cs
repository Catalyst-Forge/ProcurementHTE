using ProcurementHTE.Core.Constants;

namespace ProcurementHTE.Core.Common;

/// <summary>
/// The status in the Aksi column of LDP and Arsip LDP. A row is Done once every column of
/// the table is filled: SPMP, Job Name, Vendor, No PR, No PO, No SA, No SP3, and Submit Loket.
/// Until then it is On Progress. A cancelled job is Cancel.
/// The old LDP spreadsheet has no Submit Loket, so an archived row is Done without it.
/// "-" and "0" count as empty, as the spreadsheet writes them for a missing value.
/// </summary>
public static class LdpProgress
{
    public const string Done = "Done";
    public const string OnProgress = "On Progress";
    public const string Cancel = "Cancel";

    public static string Of(
        string? spmp,
        string? jobName,
        string? vendor,
        string? pr,
        string? po,
        string? sa,
        string? sp3,
        DateTime? submitLoket,
        bool archived,
        bool cancelled = false
    )
    {
        if (cancelled)
            return Cancel;

        var filled = new[] { spmp, jobName, vendor, pr, po, sa, sp3 }.All(LdpArchiveColumns.IsFilled)
            && (archived || submitLoket.HasValue);
        return filled ? Done : OnProgress;
    }
}
