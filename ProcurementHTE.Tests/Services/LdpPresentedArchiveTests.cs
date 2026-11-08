using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class LdpPresentedArchiveTests : IDisposable
{
    private readonly AppDbContext _db = new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options
    );

    public LdpPresentedArchiveTests()
    {
        _db.LdpArchiveRows.AddRange(
            Row("Standby Agustus", new DateTime(2026, 8, 15), "WO-AUG"),
            Row("Angkut 2025", new DateTime(2025, 8, 15), "WO-2025"),
            Row("Angkut Oktober", new DateTime(2026, 10, 1), "WO-OCT")
        );
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static LdpArchiveRow Row(string job, DateTime start, string wo) =>
        new()
        {
            ImportBatch = "test",
            JobName = job,
            StartDate = start,
            Wo = wo,
            NoPr = "PR-1",
            YearJob = "2026",
            ValuesJson = """[{"label":"Aprvl MGR · Selesai","type":"date","value":"2026-08-20T00:00:00"},{"label":"Aprvl MGR · Selesai (Invoice)","type":"date","value":"2026-10-01T00:00:00"},{"label":"Project","type":"text","value":"Rokan"}]""",
        };

    private sealed class NoSort : IListSortProvider { public ListSort? Current => null; }
    private sealed class NoFilters : IListFilterProvider { public IReadOnlyDictionary<string, string> Filters { get; } = new Dictionary<string, string>(); }

    private LdpRepository Ldp() => new(_db, new NoSort(), new NoFilters());

    [Fact]
    public async Task Jobs_that_started_july_to_september_2026_show_in_the_ldp()
    {
        var (items, total) = await Ldp().GetAllAsync(1, 25);

        var item = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal("Standby Agustus", item.TextPekerjaan);
        Assert.NotNull(item.ArchiveRowId);
        Assert.Equal(2026, item.YearJob);
        Assert.Equal("Rokan", item.ProjectRegion);
        Assert.Equal(new DateTime(2026, 8, 20), item.ManagerApprovalSelesai);
    }

    [Fact]
    public async Task The_ldp_search_also_reaches_those_jobs()
    {
        Assert.Single((await Ldp().GetAllAsync(1, 25, "wo-aug")).Items);
        Assert.Empty((await Ldp().GetAllAsync(1, 25, "tidak ada")).Items);
    }

    [Fact]
    public async Task Arsip_ldp_keeps_every_other_job_and_leaves_those_to_the_ldp()
    {
        var archive = new LdpArchiveRepository(_db);

        var page = await archive.SearchAsync(new LdpArchiveQuery(null, new Dictionary<string, string>(), null, 1, 25));
        var export = await archive.ExportAsync(new LdpArchiveQuery(null, new Dictionary<string, string>(), null, 1, int.MaxValue));

        Assert.Equal(["Angkut 2025", "Angkut Oktober"], page.Items.Select(item => item.JobName).Order());
        Assert.Equal(2, export.Count);
    }
}
