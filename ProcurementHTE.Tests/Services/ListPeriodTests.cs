using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class ListPeriodTests
{
    private static readonly DateTime?[] Starts =
    [
        new DateTime(2025, 11, 3), new DateTime(2026, 10, 31, 17, 0, 0), new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), null,
    ];

    [Theory]
    [InlineData("11", "2026", 11, 2026)]
    [InlineData("", "2026", null, 2026)]
    [InlineData("7", "", 7, null)]
    [InlineData("13", "abc", null, null)]
    [InlineData("0", "1800", null, null)]
    [InlineData(null, null, null, null)]
    public void Read_keeps_only_a_real_month_and_year(string? month, string? year, int? expectedMonth, int? expectedYear) =>
        Assert.Equal(new ListPeriod(expectedMonth, expectedYear), ListPeriod.Read(month, year));

    [Theory]
    [InlineData(null, null, 5)]
    [InlineData(null, 2026, 3)]
    [InlineData(11, 2026, 2)]
    [InlineData(11, null, 3)]
    [InlineData(10, 2026, 1)]
    public void Contains_takes_a_year_a_month_of_a_year_or_a_month_in_every_year(int? month, int? year, int expected) =>
        Assert.Equal(expected, Starts.Count(start => new ListPeriod(month, year).Contains(start)));

    [Theory]
    [InlineData(null, 2026, 3)]
    [InlineData(11, 2026, 2)]
    [InlineData(11, null, 3)]
    public void Apply_filters_the_same_rows_as_contains(int? month, int? year, int expected)
    {
        var rows = Starts.Select((start, i) => new LdpArchiveRow { ImportBatch = "x", SourceRow = i, StartDate = start }).AsQueryable();

        Assert.Equal(expected, new ListPeriod(month, year).Apply(rows, row => row.StartDate).Count());
    }

    [Fact]
    public void Month_names_read_in_indonesian() =>
        Assert.Equal(["Januari", "Oktober", "Desember"], new[] { 0, 9, 11 }.Select(i => ListPeriod.MonthNames[i]));

    [Fact]
    public void A_finance_table_keeps_the_period_with_its_other_filters()
    {
        FinanceTableRow Row(string id, DateTime? start, string job) => new() { ProcurementId = id, StartDate = start, JobName = job };
        var rows = new[] { Row("a", new DateTime(2026, 11, 2), "Angkut rig"), Row("b", new DateTime(2026, 10, 2), "Angkut rig"), Row("c", new DateTime(2026, 11, 9), "Sewa crane") };

        var page = FinanceTable.Query(rows, "angkut", new Dictionary<string, string>(), null, 1, 10, new ListPeriod(11, 2026));

        Assert.Equal(["a"], page.Items.Select(row => row.ProcurementId));
        Assert.Equal(1, page.Total);
    }

    [Fact]
    public async Task Arsip_ldp_keeps_the_period_with_its_other_filters()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.LdpArchiveRows.AddRange(
            new LdpArchiveRow { ImportBatch = "x", SourceRow = 1, JobName = "Angkut 2024", VendorName = "PT A", StartDate = new DateTime(2024, 3, 1) },
            new LdpArchiveRow { ImportBatch = "x", SourceRow = 2, JobName = "Angkut 2025", VendorName = "PT A", StartDate = new DateTime(2025, 3, 1) },
            new LdpArchiveRow { ImportBatch = "x", SourceRow = 3, JobName = "Sewa 2025", VendorName = "PT B", StartDate = new DateTime(2025, 4, 1) }
        );
        await db.SaveChangesAsync();

        var result = await new LdpArchiveRepository(db).SearchAsync(
            new LdpArchiveQuery(null, new Dictionary<string, string> { ["VendorName"] = "PT A" }, null, 1, 25, new ListPeriod(null, 2025)));

        Assert.Equal(["Angkut 2025"], result.Items.Select(item => item.JobName));
    }

    private sealed record NoSort : IListSortProvider
    {
        public ListSort? Current => null;
    }

    private sealed record Picked(ListPeriod Period) : IListFilterProvider
    {
        public IReadOnlyDictionary<string, string> Filters { get; } = new Dictionary<string, string>();
    }

    [Fact]
    public async Task The_ldp_counts_only_jobs_started_in_the_period()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Procurements.AddRange(
            new Procurement { ProcNum = "1", Wonum = "WO-A", StartDate = new DateTime(2026, 11, 2) },
            new Procurement { ProcNum = "2", Wonum = "WO-B", StartDate = new DateTime(2026, 10, 2) },
            new Procurement { ProcNum = "3", Wonum = "WO-C", StartDate = new DateTime(2025, 11, 2) }
        );
        await db.SaveChangesAsync();

        var (items, total) = await new LdpRepository(db, new NoSort(), new Picked(new ListPeriod(11, null))).GetAllAsync(1, 10);

        Assert.Equal(2, total);
        Assert.Equal(["WO-A", "WO-C"], items.Select(item => item.NoWo).Order());
    }
}

public class ListPeriodSqlServerTests
{
    // Constitution II: InMemory does not prove a query translates, so print it with the SQL Server provider.
    private static AppDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    private static string Statement(string sql) => sql[sql.IndexOf("SELECT", StringComparison.Ordinal)..];

    [Fact]
    public void A_year_or_a_month_of_a_year_is_a_date_range_with_parameters()
    {
        using var db = SqlServer();

        var year = Statement(new ListPeriod(null, 2026).Apply(db.Procurements, p => p.StartDate).ToQueryString());
        var month = Statement(new ListPeriod(11, 2026).Apply(db.LdpArchiveRows, r => r.StartDate).ToQueryString());

        Assert.Contains("[p].[StartDate] >= @", year);
        Assert.Contains("[p].[StartDate] < @", year);
        Assert.Contains("[l].[StartDate] >= @", month);
        Assert.DoesNotContain("2026", year + month);
    }

    [Fact]
    public void A_month_alone_reads_the_month_part()
    {
        using var db = SqlServer();

        var sql = new ListPeriod(11, null).Apply(db.LdpArchiveRows, r => r.StartDate).ToQueryString();

        Assert.Contains("DATEPART(month, [l].[StartDate])", sql);
    }

    [Fact]
    public void The_years_offered_are_distinct_and_newest_first()
    {
        using var db = SqlServer();

        var procurements = ListPeriodYears.ProcurementYears(db).ToQueryString();
        var archive = ListPeriodYears.ArchiveYears(db).ToQueryString();

        Assert.Contains("DATEPART(year, [p].[StartDate])", procurements);
        Assert.Contains("DISTINCT", procurements);
        Assert.Contains("DATEPART(year, [l].[StartDate])", archive);
        Assert.Contains("DESC", archive);
    }
}

public class PeriodFilterViewComponentTests
{
    private sealed class Years(int[] procurements, int[] archive) : ProcurementHTE.Core.Interfaces.IListPeriodYears
    {
        public Task<IReadOnlyList<int>> ProcurementYearsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<int>>(procurements);
        public Task<IReadOnlyList<int>> ArchiveYearsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<int>>(archive);
    }

    private static async Task<ProcurementHTE.Web.ViewComponents.PeriodFilterViewModel> Render(string query, string source, bool standalone = false)
    {
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString(query);
        var component = new ProcurementHTE.Web.ViewComponents.PeriodFilterViewComponent(new Years([2026, 2025], [2025, 2024]))
        {
            ViewComponentContext = new Microsoft.AspNetCore.Mvc.ViewComponents.ViewComponentContext
            {
                ViewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext { HttpContext = http },
            },
        };

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult>(await component.InvokeAsync(source, standalone));
        return Assert.IsType<ProcurementHTE.Web.ViewComponents.PeriodFilterViewModel>(result.ViewData!.Model);
    }

    [Theory]
    [InlineData("procurements", new[] { 2026, 2025 })]
    [InlineData("archive", new[] { 2025, 2024 })]
    [InlineData("ldp", new[] { 2026, 2025, 2024 })]
    public async Task Each_list_offers_the_years_its_jobs_started_newest_first(string source, int[] expected) =>
        Assert.Equal(expected, (await Render("", source)).Years);

    [Fact]
    public async Task The_picked_period_stays_picked_even_for_a_year_without_jobs()
    {
        var model = await Render("?bulan=11&tahun=2019&search=angkut", "procurements", standalone: true);

        Assert.Equal(new ListPeriod(11, 2019), model.Current);
        Assert.Equal([2026, 2025, 2019], model.Years);
        Assert.True(model.Standalone);
    }
}
