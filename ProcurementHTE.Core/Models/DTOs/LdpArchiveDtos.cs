using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>An archive row without its full spreadsheet values. Column keys are property names.</summary>
public sealed class LdpArchiveListItem
{
    public string Id { get; init; } = default!;
    public int SourceRow { get; init; }
    public string? Wo { get; init; }
    public string? NoSpk { get; init; }
    public string? VendorName { get; init; }
    public string? JobName { get; init; }
    public string? JobType { get; init; }
    public string? ContractType { get; init; }
    public string? LtcName { get; init; }
    public string? YearJob { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? NilaiPnl { get; init; }
    public decimal? NilaiRealisasi { get; init; }
    public string? NoSpmp { get; init; }
    public string? NoPr { get; init; }
    public DateTime? PrCreatedAt { get; init; }
    public string? NoPo { get; init; }
    public string? NoSa { get; init; }
    public string? NoSp3 { get; init; }
    public string? PicPr { get; init; }
    public string? PicOps { get; init; }
    public string? Status { get; init; }

    /// <summary>
    /// The spreadsheet's "Project" value. It lives in the row's full values, which are too large
    /// to load for every row, so it is filled for the page shown only and is not sorted or searched.
    /// </summary>
    public string? ProjectRegion { get; set; }

    /// <summary>Done or On Progress, as in the LDP; see <see cref="LdpProgress"/>. Sorts and filters as a column.</summary>
    public string Progress => LdpProgress.Of(NoSpmp, JobName, VendorName, NoPr, NoPo, NoSa, NoSp3, null, archived: true);
}

public sealed record LdpArchiveQuery(
    string? Search,
    IReadOnlyDictionary<string, string> Filters,
    ListSort? Sort,
    int Page,
    int PageSize
);
