using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class LdpArchiveRangeTests
{
    private static readonly LdpArchiveRange JulyToOctober = new(new DateTime(2026, 7, 1), new DateTime(2026, 10, 31));

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // --- The range itself ---

    [Theory]
    [InlineData(null, "2026-09-30", "Tanggal awal wajib diisi.")]
    [InlineData("2026-07-01", null, "Tanggal akhir wajib diisi.")]
    [InlineData("2026-10-31", "2026-07-01", "Tanggal awal tidak boleh sesudah tanggal akhir.")]
    [InlineData("2026-07-01", "2026-07-01", null)]
    [InlineData("2026-07-01 18:00", "2026-07-01 06:00", null)]
    public void Validate_names_what_is_wrong(string? from, string? to, string? message) =>
        Assert.Equal(message, LdpArchiveRange.Validate(Date(from), Date(to)));

    [Theory]
    [InlineData("2026-06-30 23:59", false)]
    [InlineData("2026-07-01", true)]
    [InlineData("2026-09-30 23:59", true)]
    [InlineData("2026-10-01", false)]
    [InlineData(null, false)]
    public void Both_ends_are_included_and_only_the_date_counts(string? start, bool shown) =>
        Assert.Equal(shown, LdpArchiveRange.Default.Contains(Date(start)));

    [Theory]
    [InlineData("2026-07-01", "2026-09-30", "1 Juli–30 September 2026")]
    [InlineData("2026-12-01", "2027-01-31", "1 Desember 2026–31 Januari 2027")]
    [InlineData("2026-10-01", "2026-10-01", "1 Oktober 2026")]
    public void Text_reads_in_indonesian(string from, string to, string text) =>
        Assert.Equal(text, new LdpArchiveRange(Date(from)!.Value, Date(to)!.Value).Text);

    [Theory]
    [InlineData("2026-07-01", "2026-10-31", true)]
    [InlineData("2026-06-01", "2026-09-30", true)]
    [InlineData("2026-08-01", "2026-09-30", false)]
    [InlineData("2026-07-01", "2026-09-30", false)]
    public void Widens_when_a_day_is_added(string from, string to, bool widens) =>
        Assert.Equal(widens, new LdpArchiveRange(Date(from)!.Value, Date(to)!.Value).Widens(LdpArchiveRange.Default));

    [Fact]
    public void A_range_keeps_dates_only_so_equal_days_are_equal() =>
        Assert.Equal(LdpArchiveRange.Default, new LdpArchiveRange(new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 9, 30, 17, 0, 0)));

    // --- The stored range ---

    [Fact]
    public async Task With_no_change_the_default_range_holds()
    {
        await using var db = NewDb();

        Assert.Equal(LdpArchiveRange.Default, await new LdpArchiveRangeRepository(db).GetAsync());
    }

    [Fact]
    public async Task The_newest_change_is_the_range_in_force_and_names_its_author()
    {
        await using var db = NewDb();
        db.Users.Add(new User { Id = "admin-1", UserName = "diah", FirstName = "Diah", LastName = "" });
        await db.SaveChangesAsync();
        var repository = new LdpArchiveRangeRepository(db);

        await repository.AddAsync(Change(JulyToOctober, LdpArchiveRange.Default, new DateTime(2026, 11, 9, 1, 0, 0)));
        await repository.AddAsync(Change(LdpArchiveRange.Default, JulyToOctober, new DateTime(2026, 11, 9, 2, 0, 0)));

        Assert.Equal(LdpArchiveRange.Default, await repository.GetAsync());
        var history = await repository.HistoryAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(JulyToOctober, history[0].PreviousRange);
        Assert.All(history, change => Assert.Equal("diah", change.ChangedByName));
    }

    [Fact]
    public async Task An_unknown_author_is_kept_by_id()
    {
        await using var db = NewDb();
        var repository = new LdpArchiveRangeRepository(db);

        await repository.AddAsync(Change(JulyToOctober, LdpArchiveRange.Default, DateTime.UtcNow));

        Assert.Equal("admin-1", (await repository.HistoryAsync()).Single().ChangedByName);
    }

    // --- Saving a new range ---

    private sealed class MemoryRanges : ILdpArchiveRangeRepository
    {
        public List<LdpArchiveRangeChange> Changes { get; } = [];

        public Task<LdpArchiveRange> GetAsync(CancellationToken ct = default) =>
            Task.FromResult(Changes.LastOrDefault()?.Range ?? LdpArchiveRange.Default);

        public Task<IReadOnlyList<LdpArchiveRangeChange>> HistoryAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LdpArchiveRangeChange>>(Changes.AsEnumerable().Reverse().ToList());

        public Task AddAsync(LdpArchiveRangeChange change, CancellationToken ct = default)
        {
            change.ChangedByName = change.ChangedByUserId;
            Changes.Add(change);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task An_invalid_range_is_refused_and_not_recorded()
    {
        var ranges = new MemoryRanges();

        var result = await new LdpArchiveRangeService(ranges).ChangeAsync(new DateTime(2026, 10, 31), new DateTime(2026, 7, 1), "admin-1");

        Assert.Equal("Tanggal awal tidak boleh sesudah tanggal akhir.", Assert.IsType<LdpArchiveRangeResult.Invalid>(result).Message);
        Assert.Empty(ranges.Changes);
    }

    [Fact]
    public async Task A_new_range_is_recorded_with_the_range_it_replaces()
    {
        var ranges = new MemoryRanges();

        var result = await new LdpArchiveRangeService(ranges).ChangeAsync(JulyToOctober.From, JulyToOctober.To, "admin-1");

        var changed = Assert.IsType<LdpArchiveRangeResult.Changed>(result);
        Assert.Equal(JulyToOctober, changed.Range);
        Assert.True(changed.Widened);
        var change = Assert.Single(ranges.Changes);
        Assert.Equal(LdpArchiveRange.Default, change.PreviousRange);
        Assert.Equal("admin-1", change.ChangedByUserId);
    }

    [Fact]
    public async Task Narrowing_is_not_widening()
    {
        var ranges = new MemoryRanges();

        var result = await new LdpArchiveRangeService(ranges).ChangeAsync(new DateTime(2026, 8, 1), new DateTime(2026, 9, 30), "admin-1");

        Assert.False(Assert.IsType<LdpArchiveRangeResult.Changed>(result).Widened);
    }

    [Fact]
    public async Task Saving_the_range_in_force_records_nothing()
    {
        var ranges = new MemoryRanges();
        var service = new LdpArchiveRangeService(ranges);

        await service.ChangeAsync(JulyToOctober.From, JulyToOctober.To, "admin-1");
        var again = await service.ChangeAsync(JulyToOctober.From, JulyToOctober.To.AddHours(8), "admin-2");

        Assert.IsType<LdpArchiveRangeResult.Unchanged>(again);
        Assert.Single(ranges.Changes);
    }

    [Fact]
    public async Task Two_changes_list_newest_first_each_with_the_range_it_replaced()
    {
        var ranges = new MemoryRanges();
        var service = new LdpArchiveRangeService(ranges);
        var october = new LdpArchiveRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));

        await service.ChangeAsync(JulyToOctober.From, JulyToOctober.To, "admin-1");
        await service.ChangeAsync(october.From, october.To, "admin-1");

        var history = await service.HistoryAsync();
        Assert.Equal([october, JulyToOctober], history.Select(change => change.Range));
        Assert.Equal(JulyToOctober, history[0].PreviousRange);
        Assert.Equal(LdpArchiveRange.Default, history[1].PreviousRange);
    }

    // --- Every archived job shows in exactly one list ---

    private sealed record NoSort : IListSortProvider
    {
        public ListSort? Current => null;
    }

    private sealed record NoFilters : IListFilterProvider
    {
        public IReadOnlyDictionary<string, string> Filters { get; } = new Dictionary<string, string>();
    }

    public static TheoryData<string, string> Ranges => new()
    {
        { "2026-07-01", "2026-09-30" },
        { "2026-07-01", "2026-10-31" },
        { "2026-10-01", "2026-10-31" },
    };

    [Theory]
    [MemberData(nameof(Ranges))]
    public async Task Every_archived_job_shows_in_the_ldp_or_in_arsip_ldp_never_both(string from, string to)
    {
        await using var db = NewDb();
        string?[] starts = ["2026-06-30", "2026-07-01", "2026-09-30", "2026-10-01", "2026-10-31", null];
        db.LdpArchiveRows.AddRange(starts.Select((start, i) => new LdpArchiveRow
        {
            ImportBatch = "test",
            SourceRow = i + 1,
            JobName = $"Job {i + 1}",
            StartDate = Date(start),
        }));
        await db.SaveChangesAsync();
        var range = new LdpArchiveRange(Date(from)!.Value, Date(to)!.Value);
        if (range != LdpArchiveRange.Default)
            await new LdpArchiveRangeRepository(db).AddAsync(Change(range, LdpArchiveRange.Default, DateTime.UtcNow));

        var (ldp, _) = await new LdpRepository(db, new NoSort(), new NoFilters()).GetAllAsync(1, 100);
        var arsip = await new LdpArchiveRepository(db).SearchAsync(new LdpArchiveQuery(null, new Dictionary<string, string>(), null, 1, 100));

        var inLdp = ldp.Where(item => item.ArchiveRowId is not null).Select(item => item.TextPekerjaan!).ToHashSet();
        var inArsip = arsip.Items.Select(item => item.JobName!).ToHashSet();
        Assert.Equal(starts.Length, inLdp.Count + inArsip.Count);
        Assert.Empty(inLdp.Intersect(inArsip));
        Assert.Equal(
            starts.Select((start, i) => (start, name: $"Job {i + 1}")).Where(row => range.Contains(Date(row.start))).Select(row => row.name).ToHashSet(),
            inLdp
        );
    }

    internal static LdpArchiveRangeChange Change(LdpArchiveRange range, LdpArchiveRange previous, DateTime at) => new()
    {
        StartFrom = range.From,
        StartTo = range.To,
        PreviousStartFrom = previous.From,
        PreviousStartTo = previous.To,
        ChangedByUserId = "admin-1",
        ChangedAt = at,
    };

    private static DateTime? Date(string? text) => text is null ? null : DateTime.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}

public class LdpArchiveRangeSqlServerTests
{
    // Constitution II: InMemory does not prove a query translates, so print it with the SQL Server provider.
    private static AppDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    private static readonly LdpArchiveRange Range = new(new DateTime(2026, 7, 1), new DateTime(2026, 10, 31));

    [Fact]
    public void The_range_in_force_is_one_ordered_row()
    {
        using var db = SqlServer();
        var sql = LdpArchiveRangeRepository.Newest(db)
            .Select(change => new { change.StartFrom, change.StartTo })
            .Take(1)
            .ToQueryString();

        Assert.Contains("SELECT TOP(", sql);
        Assert.Contains("ORDER BY [l].[ChangedAt] DESC, [l].[LdpArchiveRangeChangeId] DESC", sql);
    }

    [Fact]
    public void Both_halves_of_the_archive_translate_with_the_dates_as_parameters()
    {
        using var db = SqlServer();

        // ToQueryString prints the parameter values as DECLAREs; the statement itself starts at SELECT.
        static string Statement(string sql) => sql[sql.IndexOf("SELECT", StringComparison.Ordinal)..];
        var inside = Statement(LdpArchiveRangeRepository.InRange(db.LdpArchiveRows, Range).ToQueryString());
        var outside = Statement(LdpArchiveRangeRepository.OutsideRange(db.LdpArchiveRows, Range).ToQueryString());

        Assert.Contains("[l].[StartDate] IS NOT NULL", inside);
        Assert.Contains("[l].[StartDate] >= @", inside);
        Assert.Contains("[l].[StartDate] < @", inside);
        Assert.Contains("[l].[StartDate] IS NULL", outside);
        Assert.DoesNotContain("2026", inside + outside);
    }
}
