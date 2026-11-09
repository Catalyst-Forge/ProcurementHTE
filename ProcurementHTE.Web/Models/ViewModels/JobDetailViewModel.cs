using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels;

/// <summary>
/// One job's detail page: every LDP column and its tracking. A Docutrax job carries the tracking
/// of the Tracking page; an Arsip LDP row carries stages built from its spreadsheet dates and
/// every spreadsheet value.
/// </summary>
public sealed class JobDetailViewModel
{
    public required LdpRecapDto Job { get; init; }
    public ProcurementTrackingDto? Tracking { get; init; }
    public IReadOnlyList<LdpTrackingStep>? ArchiveSteps { get; init; }
    public IReadOnlyList<LdpArchiveValue>? ExcelValues { get; init; }
    public JobDetailOrigin Origin { get; init; } = JobDetailOrigin.Ldp;

    public bool IsArchived => Job.ArchiveRowId is not null;
}

/// <summary>
/// The list a detail page was opened from, which its back button returns to. Only these lists
/// are known, so the button never leads to an address a user typed.
/// </summary>
public sealed record JobDetailOrigin(string Key, string Controller, string Label)
{
    public static readonly JobDetailOrigin Ldp = new("ldp", "Ldp", "Kembali ke LDP");
    public static readonly JobDetailOrigin Arsip = new("arsip", "LdpArchive", "Kembali ke Arsip LDP");
    public static readonly JobDetailOrigin ApPo = new("appo", "ApPoPickup", "Kembali ke AP Purchase Order");
    public static readonly JobDetailOrigin ApInvoice = new("apinvoice", "ApInvoicePickup", "Kembali ke AP Invoice");
    public static readonly JobDetailOrigin Accrual = new("accrual", "Accrual", "Kembali ke Data Accrual");

    private static readonly JobDetailOrigin[] All = [Ldp, Arsip, ApPo, ApInvoice, Accrual];

    /// <summary>The list named by <paramref name="from"/>, or <paramref name="fallback"/> (LDP when not given).</summary>
    public static JobDetailOrigin From(string? from, JobDetailOrigin? fallback = null) =>
        All.FirstOrDefault(origin => string.Equals(origin.Key, from, StringComparison.OrdinalIgnoreCase)) ?? fallback ?? Ldp;
}
