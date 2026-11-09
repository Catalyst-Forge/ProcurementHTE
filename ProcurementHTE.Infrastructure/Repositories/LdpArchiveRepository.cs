using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

/// <summary>
/// The archive holds a few thousand rows, so search, filters, and sorting run in memory on
/// the text the table shows, the same way the LDP does.
/// </summary>
public sealed class LdpArchiveRepository(AppDbContext db) : ILdpArchiveRepository
{
    private static readonly CultureInfo Indonesian = new("id-ID");

    private static readonly PropertyInfo[] SearchedColumns =
    [
        .. new[]
        {
            nameof(LdpArchiveListItem.Wo), nameof(LdpArchiveListItem.NoSpk), nameof(LdpArchiveListItem.VendorName),
            nameof(LdpArchiveListItem.JobName), nameof(LdpArchiveListItem.LtcName), nameof(LdpArchiveListItem.NoSpmp),
            nameof(LdpArchiveListItem.NoPr), nameof(LdpArchiveListItem.NoPo), nameof(LdpArchiveListItem.NoSa),
            nameof(LdpArchiveListItem.NoSp3), nameof(LdpArchiveListItem.PicPr), nameof(LdpArchiveListItem.PicOps),
        }.Select(name => typeof(LdpArchiveListItem).GetProperty(name)!),
    ];

    public async Task<Core.Common.PagedResult<LdpArchiveListItem>> SearchAsync(LdpArchiveQuery query, CancellationToken ct = default)
    {
        var items = await (await NotInLdpAsync(ct))
            .Select(row => new LdpArchiveListItem
            {
                Id = row.LdpArchiveRowId,
                SourceRow = row.SourceRow,
                Wo = row.Wo,
                NoSpk = row.NoSpk,
                VendorName = row.VendorName,
                JobName = row.JobName,
                JobType = row.JobType,
                ContractType = row.ContractType,
                LtcName = row.LtcName,
                YearJob = row.YearJob,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                NilaiPnl = row.NilaiPnl,
                NilaiRealisasi = row.NilaiRealisasi,
                NoSpmp = row.NoSpmp,
                NoPr = row.NoPr,
                PrCreatedAt = row.PrCreatedAt,
                NoPo = row.NoPo,
                NoSa = row.NoSa,
                NoSp3 = row.NoSp3,
                PicPr = row.PicPr,
                PicOps = row.PicOps,
                Status = row.Status,
            })
            .ToListAsync(ct);

        var all = Arrange(items, query);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);

        var shown = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        await FillProjectRegionAsync(shown, ct);

        return new Core.Common.PagedResult<LdpArchiveListItem>
        {
            Items = shown,
            Page = page,
            PageSize = pageSize,
            Total = all.Count,
        };
    }

    // Reads "Project" from the full values of the rows on the page only.
    private async Task FillProjectRegionAsync(List<LdpArchiveListItem> items, CancellationToken ct)
    {
        var ids = items.Select(item => item.Id).ToList();
        var values = await db.LdpArchiveRows.AsNoTracking()
            .Where(row => ids.Contains(row.LdpArchiveRowId))
            .Select(row => new { row.LdpArchiveRowId, row.ValuesJson })
            .ToDictionaryAsync(row => row.LdpArchiveRowId, row => row.ValuesJson, ct);

        foreach (var item in items)
        {
            item.ProjectRegion = LdpArchiveEditor.Read(values.GetValueOrDefault(item.Id))
                .FirstOrDefault(cell => cell.Label == "Project")?.Value?.ToString();
        }
    }

    public async Task<IReadOnlyList<LdpArchiveRow>> ExportAsync(LdpArchiveQuery query, CancellationToken ct = default)
    {
        var rows = await (await NotInLdpAsync(ct)).ToDictionaryAsync(row => row.LdpArchiveRowId, ct);
        var items = rows.Values.Select(row => new LdpArchiveListItem
        {
            Id = row.LdpArchiveRowId,
            SourceRow = row.SourceRow,
            Wo = row.Wo,
            NoSpk = row.NoSpk,
            VendorName = row.VendorName,
            JobName = row.JobName,
            JobType = row.JobType,
            ContractType = row.ContractType,
            LtcName = row.LtcName,
            YearJob = row.YearJob,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            NilaiPnl = row.NilaiPnl,
            NilaiRealisasi = row.NilaiRealisasi,
            NoSpmp = row.NoSpmp,
            NoPr = row.NoPr,
            PrCreatedAt = row.PrCreatedAt,
            NoPo = row.NoPo,
            NoSa = row.NoSa,
            NoSp3 = row.NoSp3,
            PicPr = row.PicPr,
            PicOps = row.PicOps,
            Status = row.Status,
        });

        return Arrange(items, query).Select(item => rows[item.Id]).ToList();
    }

    // Jobs that started within the LDP range are shown in the LDP instead.
    private async Task<IQueryable<LdpArchiveRow>> NotInLdpAsync(CancellationToken ct) =>
        LdpArchiveRangeRepository.OutsideRange(
            db.LdpArchiveRows.AsNoTracking(),
            await LdpArchiveRangeRepository.CurrentAsync(db, ct)
        );

    public Task<LdpArchiveRow?> FindForEditAsync(string id, CancellationToken ct = default) =>
        db.LdpArchiveRows.FirstOrDefaultAsync(row => row.LdpArchiveRowId == id, ct);

    public void Add(LdpArchiveRow row) => db.LdpArchiveRows.Add(row);

    public void Remove(LdpArchiveRow row) => db.LdpArchiveRows.Remove(row);

    public Task SaveAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static List<LdpArchiveListItem> Arrange(IEnumerable<LdpArchiveListItem> items, LdpArchiveQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(item => SearchedColumns.Any(column => Shows(column.GetValue(item), search)));
        }

        if (query.Period is { IsEmpty: false } period)
            items = items.Where(item => period.Contains(item.StartDate));

        foreach (var (key, value) in query.Filters)
        {
            if (FindColumn(key) is { } column)
                items = items.Where(item => Shows(column.GetValue(item), value));
        }

        return Sort(items, FindColumn(query.Sort?.Key) ?? DefaultSort, query.Sort is null || query.Sort.Descending);
    }

    private static readonly PropertyInfo DefaultSort = typeof(LdpArchiveListItem).GetProperty(nameof(LdpArchiveListItem.PrCreatedAt))!;

    private static List<LdpArchiveListItem> Sort(IEnumerable<LdpArchiveListItem> items, PropertyInfo column, bool descending)
    {
        var comparer = Comparer<object?>.Create(CompareValues);
        return (descending ? items.OrderByDescending(column.GetValue, comparer) : items.OrderBy(column.GetValue, comparer))
            .ThenBy(item => item.SourceRow)
            .ToList();
    }

    public Task<LdpArchiveRow?> GetAsync(string id, CancellationToken ct = default) =>
        db.LdpArchiveRows.AsNoTracking().FirstOrDefaultAsync(row => row.LdpArchiveRowId == id, ct);

    public static PropertyInfo? FindColumn(string? key) =>
        key is null || key == nameof(LdpArchiveListItem.Id)
            ? null
            : typeof(LdpArchiveListItem).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    // Matches what the table shows, so a search finds what the user can see.
    private static bool Shows(object? value, string text) =>
        value switch
        {
            null => false,
            DateTime date => date.ToString("dd MMM yyyy", Indonesian).Contains(text, StringComparison.OrdinalIgnoreCase),
            decimal number => number.ToString("N0", Indonesian).Contains(text, StringComparison.OrdinalIgnoreCase)
                || number.ToString("0", CultureInfo.InvariantCulture).Contains(text, StringComparison.OrdinalIgnoreCase),
            _ => value.ToString()!.Contains(text, StringComparison.OrdinalIgnoreCase),
        };

    // Empty values sort lowest, as NULL does in the lists sorted by SQL Server.
    private static int CompareValues(object? left, object? right) =>
        (left, right) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase),
            _ => Comparer<object>.Default.Compare(left, right),
        };
}
