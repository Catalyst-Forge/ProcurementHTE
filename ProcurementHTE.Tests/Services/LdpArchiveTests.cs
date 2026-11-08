using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Tests.Services;

public class LdpArchiveTests : IDisposable
{
    private readonly AppDbContext _db = new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options
    );
    private readonly LdpArchiveRepository _archive;

    public LdpArchiveTests()
    {
        _archive = new LdpArchiveRepository(_db);
        _db.LdpArchiveRows.AddRange(
            Row(1, "PT Kurnia Jaya", "Pengangkutan buku", "PR-100", 13_000_000m, new DateTime(2025, 7, 1)),
            Row(2, "PT Kurnia Jaya", "Pengangkutan hp", "PR-100", 18_000_000m, new DateTime(2025, 8, 1)),
            Row(3, "CV Sinar", "Standby rig", "PR-200", null, new DateTime(2025, 9, 1)),
            Row(4, "CV Sinar", "Moving rig", "PR-300", 50_000_000m, null)
        );
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static LdpArchiveRow Row(int source, string vendor, string job, string pr, decimal? pnl, DateTime? prDate) =>
        new()
        {
            ImportBatch = "test",
            SourceRow = source,
            VendorName = vendor,
            JobName = job,
            NoPr = pr,
            NilaiPnl = pnl,
            PrCreatedAt = prDate,
            ValuesJson = """[{"label":"Nama Vendor","type":"text","value":"x"}]""",
        };

    private Task<ProcurementHTE.Core.Common.PagedResult<LdpArchiveListItem>> Search(
        string? search = null,
        Dictionary<string, string>? filters = null,
        ListSort? sort = null,
        int page = 1,
        int pageSize = 25
    ) => _archive.SearchAsync(new LdpArchiveQuery(search, filters ?? [], sort, page, pageSize));

    [Fact]
    public async Task Search_looks_across_vendor_job_and_numbers_ignoring_case()
    {
        Assert.Equal(2, (await Search("kurnia")).Total);
        Assert.Equal(2, (await Search("pr-100")).Total);
        Assert.Equal(1, (await Search("Standby")).Total);
    }

    [Fact]
    public async Task Column_filters_narrow_the_rows_and_unknown_columns_are_ignored()
    {
        var result = await Search(filters: new() { ["VendorName"] = "sinar", ["NoSuchColumn"] = "x" });

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, item => Assert.Equal("CV Sinar", item.VendorName));
    }

    [Fact]
    public async Task Empty_values_sort_lowest_like_the_lists_sorted_by_sql_server()
    {
        var descending = await Search(sort: new ListSort("nilaipnl", Descending: true));
        var ascending = await Search(sort: new ListSort("NilaiPnl", Descending: false));
        var byDefault = await Search();

        Assert.Equal([50_000_000m, 18_000_000m, 13_000_000m, null], descending.Items.Select(item => item.NilaiPnl));
        Assert.Equal([null, 13_000_000m, 18_000_000m, 50_000_000m], ascending.Items.Select(item => item.NilaiPnl));
        Assert.Equal([3, 2, 1, 4], byDefault.Items.Select(item => item.SourceRow));
    }

    [Fact]
    public async Task A_page_past_the_end_shows_the_last_page()
    {
        var result = await Search(page: 9, pageSize: 3);

        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
        Assert.Equal(4, result.Total);
    }

    [Fact]
    public void Detail_values_show_numbers_and_dates_the_indonesian_way()
    {
        var values = JsonSerializer.Deserialize<List<LdpArchiveValue>>(
            """
            [{"label":"Nilai P&L","type":"number","value":1250000},
             {"label":"Tgl. Mulai","type":"date","value":"2025-07-01"},
             {"label":"No. PR","type":"text","value":"PR-100"}]
            """,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;

        Assert.Equal(["1.250.000", "01 Jul 2025", "PR-100"], values.Select(value => value.Display));
    }
}
