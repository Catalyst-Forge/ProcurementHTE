using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

// Hapus terpilih on the LDP and Arsip LDP: the ticked rows go to Data Terhapus, the rest stay.
public class BulkDeleteTests : IDisposable
{
    private const string AdminId = "admin-1";
    private readonly ServiceProvider _services;
    private readonly AppDbContext _db;
    private readonly LdpArchiveRepository _archive;
    private readonly List<LdpArchiveRow> _rows;

    public BulkDeleteTests()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddSingleton<ICurrentUserAccessor>(new FixedUser(AdminId));
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<AppDbContext>(
            (sp, o) => o.UseInMemoryDatabase(dbName).AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>())
        );
        _services = services.BuildServiceProvider();
        _db = _services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        _archive = new LdpArchiveRepository(_db);

        _rows = Enumerable.Range(1, 4)
            .Select(n => new LdpArchiveRow { ImportBatch = "test", SourceRow = n, JobName = $"Pekerjaan {n}", ValuesJson = "[]" })
            .ToList();
        _db.LdpArchiveRows.AddRange(_rows);
        _db.SaveChanges();
    }

    public void Dispose() => _services.Dispose();

    private sealed class FixedUser(string id) : ICurrentUserAccessor
    {
        public string? UserId => id;
    }

    private async Task<int> DeleteAsync(params string[] ids)
    {
        var rows = await _archive.FindManyForEditAsync(BulkSelection.Ids(ids)!);
        foreach (var row in rows)
            _archive.Remove(row);
        await _archive.SaveAsync();
        return rows.Count;
    }

    [Fact]
    public async Task Ticked_archive_rows_move_to_deleted_records_and_the_others_stay()
    {
        var deleted = await DeleteAsync(_rows[0].LdpArchiveRowId, _rows[2].LdpArchiveRowId);

        Assert.Equal(2, deleted);
        Assert.Equal(["Pekerjaan 2", "Pekerjaan 4"], _db.LdpArchiveRows.OrderBy(r => r.SourceRow).Select(r => r.JobName).ToList());

        var gone = _db.LdpArchiveRows.IgnoreQueryFilters().Where(r => r.IsDeleted).OrderBy(r => r.SourceRow).ToList();
        Assert.Equal([1, 3], gone.Select(r => r.SourceRow));
        Assert.All(gone, row => Assert.Equal(AdminId, row.DeletedBy));
    }

    [Fact]
    public async Task A_row_already_deleted_or_unknown_is_skipped()
    {
        await DeleteAsync(_rows[0].LdpArchiveRowId);

        var deleted = await DeleteAsync(_rows[0].LdpArchiveRowId, "not-a-row", _rows[1].LdpArchiveRowId);

        Assert.Equal(1, deleted);
        Assert.Equal(2, _db.LdpArchiveRows.Count());
    }

    [Fact]
    public void A_selection_drops_blanks_and_repeats_and_keeps_the_order()
    {
        Assert.Equal(["b", "a"], BulkSelection.Ids(["b", " ", null, "a", "b", " a "]));
        Assert.Empty(BulkSelection.Ids(null)!);
    }

    [Fact]
    public void A_selection_larger_than_a_page_is_refused()
    {
        var page = Enumerable.Range(1, BulkSelection.Max).Select(n => $"id-{n}").ToList();

        Assert.Equal(BulkSelection.Max, BulkSelection.Ids(page)!.Count);
        Assert.Null(BulkSelection.Ids([.. page, "one-more"]));
    }
}
