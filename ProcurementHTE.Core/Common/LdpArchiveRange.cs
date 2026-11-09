using System.Globalization;

namespace ProcurementHTE.Core.Common;

/// <summary>
/// The start dates of archived jobs that show in the LDP instead of Arsip LDP. Both ends are
/// included and only the date counts. The importer also keeps an unfinished job only when its
/// start falls in the range. An Admin sets it from Arsip LDP; until then <see cref="Default"/> holds.
/// </summary>
public sealed record LdpArchiveRange
{
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");

    public LdpArchiveRange(DateTime from, DateTime to)
    {
        From = from.Date;
        To = to.Date;
    }

    /// <summary>The range before anyone changed it: jobs started July to September 2026.</summary>
    public static LdpArchiveRange Default { get; } = new(new DateTime(2026, 7, 1), new DateTime(2026, 9, 30));

    public DateTime From { get; }

    public DateTime To { get; }

    /// <summary>The day after <see cref="To"/>, the exclusive upper bound a query compares against.</summary>
    public DateTime Before => To.AddDays(1);

    public bool Contains(DateTime? start) => start is { } date && date.Date >= From && date.Date <= To;

    /// <summary>True when this range covers a day that <paramref name="previous"/> does not.</summary>
    public bool Widens(LdpArchiveRange previous) => From < previous.From || To > previous.To;

    /// <summary>"1 Juli–30 September 2026"; both years when they differ; one date for a single day.</summary>
    public string Text =>
        From == To ? Day(From, withYear: true)
        : From.Year == To.Year ? $"{Day(From, withYear: false)}–{Day(To, withYear: true)}"
        : $"{Day(From, withYear: true)}–{Day(To, withYear: true)}";

    private static string Day(DateTime date, bool withYear) =>
        date.ToString(withYear ? "d MMMM yyyy" : "d MMMM", Indonesian);

    /// <summary>The message for a range that cannot be saved, or null when it can.</summary>
    public static string? Validate(DateTime? from, DateTime? to) =>
        from is null ? "Tanggal awal wajib diisi."
        : to is null ? "Tanggal akhir wajib diisi."
        : from.Value.Date > to.Value.Date ? "Tanggal awal tidak boleh sesudah tanggal akhir."
        : null;
}
