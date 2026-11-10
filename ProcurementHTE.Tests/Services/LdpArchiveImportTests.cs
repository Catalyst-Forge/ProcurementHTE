using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Services;

namespace ProcurementHTE.Tests.Services;

public class LdpArchiveImportTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly AppDbContext _db;
    private readonly LdpArchiveImporter _importer;

    public LdpArchiveImportTests()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddSingleton<ICurrentUserAccessor>(new FixedUser());
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(provider.GetRequiredService<SoftDeleteInterceptor>()));
        _services = services.BuildServiceProvider();
        _db = _services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        _importer = new LdpArchiveImporter(_db);
    }

    public void Dispose() => _services.Dispose();

    private sealed class FixedUser : ICurrentUserAccessor
    {
        public string? UserId => "admin-1";
    }

    // Laid out like the real sheet: ungrouped columns left of column 33, grouped ones from it.
    private static readonly (int Column, string? Group, string Name)[] Layout =
    [
        (2, null, "No."), (3, null, "WO"), (4, null, "No. SPK"), (5, null, "Nama Vendor"), (6, null, "Tgl. Mulai"),
        (7, null, "Tgl. Selesai"), (8, null, "Nama Pekerjaan"), (9, null, "Job Type"), (10, null, "Cntract Type"),
        (11, null, "LTC Name"), (15, null, "Year Job"), (17, null, "Nilai P&L"), (18, null, "Nilai Accrual"),
        (19, null, "Nilai Realisasi"), (20, null, "No. SPMP"),
        (33, "PR", "Create"), (34, null, "No. PR"), (35, "Aprvl MGR", "Mulai"), (36, null, "Selesai"),
        (37, "PO/RO", "Tgl"), (38, null, "No.PO"), (39, "Pic PR", ""), (40, "Pic Ops", ""), (41, "Keterangan", ""),
        (42, "Tgl Process Inv Mitra", "No. Inv"), (43, null, "Nilai (IDR)"), (44, null, "No. SA"), (45, null, "No. SP3"),
        (46, "Aprvl MGR", "Selesai"),
    ];

    private static readonly Dictionary<string, object?> Done = new()
    {
        ["No."] = 1, ["WO"] = "WO-1", ["Nama Vendor"] = "PT Kurnia", ["Tgl. Mulai"] = new DateTime(2025, 3, 1),
        ["Nama Pekerjaan"] = "Angkut rig", ["Job Type"] = "Stb", ["Cntract Type"] = "Spot", ["Year Job"] = 2025,
        ["Nilai P&L"] = 1_250_000.5, ["Nilai Realisasi"] = 900_000, ["Create"] = new DateTime(2025, 2, 1),
        ["No. PR"] = "PR-100", ["No.PO"] = "PO-1", ["No. SA"] = "SA-1", ["No. SP3"] = "SP3-1", ["Pic Ops"] = "Budi",
        ["Selesai"] = new DateTime(2025, 2, 20),
    };

    private static MemoryStream Workbook(params Dictionary<string, object?>[] rows)
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Sheet1").Cell(1, 1).Value = "lain";
        var sheet = workbook.Worksheets.Add("LDP(new)");
        sheet.Cell(1, 1).Value = "LDP 2025";
        sheet.Cell(5, 18).Value = 386439267; // a column total in the group row, not a group name
        foreach (var (column, group, name) in Layout)
        {
            if (group is not null)
                sheet.Cell(5, column + 1).Value = group;
            if (name.Length > 0)
                sheet.Cell(6, column + 1).Value = name;
            sheet.Cell(7, column + 1).Value = $"Column{column}";
        }

        var rowNumber = 8;
        foreach (var row in rows)
        {
            foreach (var (column, _, name) in Layout)
            {
                var key = name.Length > 0 ? name : Layout.First(item => item.Column == column).Group!;
                if (row.TryGetValue(key, out var value) && value is not null)
                    sheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(value);
            }
            rowNumber++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static Dictionary<string, object?> With(Dictionary<string, object?> row, params (string Key, object? Value)[] changes)
    {
        var copy = new Dictionary<string, object?>(row);
        foreach (var (key, value) in changes)
            copy[key] = value;
        return copy;
    }

    private LdpImportFile Read(params Dictionary<string, object?>[] rows) => Read(LdpArchiveRange.Default, rows);

    private LdpImportFile Read(LdpArchiveRange range, params Dictionary<string, object?>[] rows) =>
        _importer.Read(Workbook(rows), "ldp.xlsx", range);

    private static Dictionary<string, object?>[] Sample() =>
    [
        Done,
        With(Done, ("No. SA", null), ("No. SP3", null), ("Tgl. Mulai", new DateTime(2026, 8, 15)), ("Nama Pekerjaan", "Standby Agustus")),
        With(Done, ("No. SA", null), ("Tgl. Mulai", new DateTime(2025, 8, 15)), ("Nama Pekerjaan", "Belum selesai 2025")),
        With(Done, ("No. SA", "SA-2"), ("Nilai Realisasi", 50_000)),
        new() { ["WO"] = "x", ["Nama Vendor"] = "y" },
        With(Done, ("No. SP3", 0), ("Nama Pekerjaan", "SP3 masih nol")),
        With(Done, ("No.PO", null), ("No. SA", null), ("No. SP3", null), ("Tgl. Mulai", new DateTime(2026, 8, 20)), ("Nama Pekerjaan", "Belum PO Agustus")),
    ];

    [Fact]
    public void Reads_done_jobs_and_the_presented_months_and_leaves_the_rest_out()
    {
        var file = Read(Sample());

        Assert.Equal(["Angkut rig", "Standby Agustus", "Angkut rig"], file.Rows.Select(row => row.JobName));
        Assert.Equal(3, file.Excluded);
        Assert.Equal([8, 9, 11], file.Rows.Select(row => row.SourceRow));
        Assert.All(file.Rows, row => Assert.Equal("Completed", row.Status));
    }

    [Fact]
    public void An_unfinished_job_is_kept_only_when_it_started_within_the_range_given()
    {
        var october = With(Done, ("No. SA", null), ("Tgl. Mulai", new DateTime(2026, 10, 15)), ("Nama Pekerjaan", "Standby Oktober"));
        var julyToOctober = new LdpArchiveRange(new DateTime(2026, 7, 1), new DateTime(2026, 10, 31));

        var widened = Read(julyToOctober, october);
        var standard = Read(october);

        Assert.Equal(["Standby Oktober"], widened.Rows.Select(row => row.JobName));
        Assert.Equal(julyToOctober, widened.Range);
        Assert.Empty(standard.Rows);
        Assert.Equal(1, standard.Excluded);
        Assert.Equal(LdpArchiveRange.Default, standard.Range);
    }

    [Fact]
    public void A_job_started_within_the_range_is_read_only_with_a_po_number()
    {
        var withPo = With(Done, ("No. SA", null), ("No. SP3", null), ("Tgl. Mulai", new DateTime(2026, 7, 10)), ("Nama Pekerjaan", "Sudah PO"));

        var file = Read(
            withPo,
            With(withPo, ("No.PO", null), ("Nama Pekerjaan", "Belum PO")),
            With(withPo, ("No.PO", "-"), ("Nama Pekerjaan", "PO strip")),
            With(withPo, ("No.PO", 0), ("Nama Pekerjaan", "PO nol"))
        );

        Assert.Equal(["Sudah PO"], file.Rows.Select(row => row.JobName));
        Assert.Equal(3, file.Excluded);
    }

    [Fact]
    public async Task A_job_in_the_ldp_has_its_manager_approval_at_most_three_days_after_it_started()
    {
        var august = With(Done, ("No. SA", null), ("No. SP3", null), ("Tgl. Mulai", new DateTime(2026, 8, 15)));
        Dictionary<string, object?>[] rows =
        [
            With(august, ("Nama Pekerjaan", "Terlambat"), ("Mulai", new DateTime(2026, 9, 1)), ("Selesai", new DateTime(2026, 9, 10))),
            With(august, ("Nama Pekerjaan", "Kosong"), ("Selesai", null)),
            With(august, ("Nama Pekerjaan", "Tepat waktu"), ("Mulai", new DateTime(2026, 8, 16)), ("Selesai", new DateTime(2026, 8, 18))),
            With(Done, ("Nama Pekerjaan", "Arsip lama"), ("Selesai", new DateTime(2025, 4, 20))),
        ];

        var file = Read(rows);
        var cells = file.Rows.ToDictionary(
            row => row.JobName!,
            row => LdpArchiveEditor.Read(row.ValuesJson).ToDictionary(cell => cell.Label, cell => cell.Value?.ToString())
        );
        var emptyLabels = cells["Kosong"].Keys.ToList();

        Assert.Equal("2026-08-18T00:00:00", cells["Terlambat"]["Aprvl MGR · Selesai"]);
        Assert.Equal("2026-08-18T00:00:00", cells["Terlambat"]["Aprvl MGR · Mulai"]);
        Assert.Equal("2026-09-10T00:00:00", cells["Terlambat"]["Aprvl MGR · Selesai (Invoice)"]);
        Assert.Equal("2026-08-18T00:00:00", cells["Kosong"]["Aprvl MGR · Selesai"]);
        Assert.False(cells["Kosong"].ContainsKey("Aprvl MGR · Mulai"));
        Assert.False(cells["Kosong"].ContainsKey("Aprvl MGR · Selesai (Invoice)"));
        Assert.InRange(emptyLabels.IndexOf("Aprvl MGR · Selesai"), emptyLabels.IndexOf("PR · No. PR") + 1, emptyLabels.IndexOf("PO/RO · No.PO") - 1);
        Assert.Equal("2026-08-16T00:00:00", cells["Tepat waktu"]["Aprvl MGR · Mulai"]);
        Assert.Equal("2026-08-18T00:00:00", cells["Tepat waktu"]["Aprvl MGR · Selesai"]);
        Assert.Equal("2025-04-20T00:00:00", cells["Arsip lama"]["Aprvl MGR · Selesai"]);

        await _importer.ApplyAsync(file, "admin-1");
        var again = await _importer.PreviewAsync(Read(rows));
        Assert.Equal((0, 0, 4), (again.New, again.Updated, again.Unchanged));
    }

    [Theory]
    [InlineData("2026-08-20", "2026-08-18")]
    [InlineData("2026-08-18", "2026-08-18")]
    [InlineData("2026-08-10", "2026-08-10")]
    [InlineData(null, "2026-08-18")]
    public void The_manager_approval_finishes_by_the_third_day(string? recorded, string expected)
    {
        var finished = LdpManagerApproval.Finished(new DateTime(2026, 8, 15), recorded is null ? null : DateTime.Parse(recorded));

        Assert.Equal(DateTime.Parse(expected), finished);
        Assert.Equal(finished, LdpManagerApproval.Started(new DateTime(2026, 9, 1), finished));
        Assert.Null(LdpManagerApproval.Started(null, finished));
    }

    [Fact]
    public void Rows_sharing_pr_job_and_start_get_their_own_key_by_order()
    {
        var file = Read(Sample());

        Assert.Equal(3, file.Rows.Select(row => row.SourceKey).Distinct().Count());
        Assert.EndsWith("-1", file.Rows[0].SourceKey);
        Assert.EndsWith("-2", file.Rows[2].SourceKey);
        Assert.Equal(file.Rows[0].SourceKey![..^2], file.Rows[2].SourceKey![..^2]);
    }

    [Fact]
    public void Typed_columns_and_grouped_labels_come_from_the_two_header_rows()
    {
        var row = Read(Done).Rows.Single();
        var labels = JsonDocument.Parse(row.ValuesJson).RootElement.EnumerateArray().Select(cell => cell.GetProperty("label").GetString()).ToList();

        Assert.Equal("StandBy", row.JobType);
        Assert.Equal("2025", row.YearJob);
        Assert.Equal(1_250_000.5m, row.NilaiPnl);
        Assert.Equal(new DateTime(2025, 2, 1), row.PrCreatedAt);
        Assert.Equal("PR-100", row.NoPr);
        Assert.Equal("Budi", row.PicOps);
        Assert.Contains("PR · No. PR", labels);
        Assert.Contains("Tgl Process Inv Mitra · No. SA", labels);
        Assert.Contains("Nilai P&L", labels);
        Assert.Contains("Aprvl MGR · Selesai", labels);
        Assert.Contains("Aprvl MGR · Selesai (Invoice)", labels);
        Assert.Equal(labels.Count, labels.Distinct().Count());
    }

    [Fact]
    public void A_sheet_without_the_ldp_tab_is_refused_with_a_message()
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Lain").Cell(1, 1).Value = "x";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<LdpImportException>(() => _importer.Read(stream, "lain.xlsx", LdpArchiveRange.Default));
        Assert.Contains("LDP(new)", error.Message);
    }

    [Fact]
    public async Task Importing_again_never_duplicates_and_the_spreadsheet_wins()
    {
        var first = await _importer.ApplyAsync(Read(Sample()), "admin-1");
        var manual = new LdpArchiveRow { ImportBatch = "Manual", JobName = "Tambahan Admin" };
        _db.LdpArchiveRows.Add(manual);
        await _db.SaveChangesAsync();

        var again = await _importer.PreviewAsync(Read(Sample()));

        var edited = await _db.LdpArchiveRows.FirstAsync(row => row.JobName == "Standby Agustus");
        edited.VendorName = "Diubah Admin";
        var deleted = await _db.LdpArchiveRows.FirstAsync(row => row.SourceRow == 11);
        _db.LdpArchiveRows.Remove(deleted);
        await _db.SaveChangesAsync();
        var changed = Sample();
        changed[0] = With(Done, ("Nilai Realisasi", 999_999));

        var third = await _importer.ApplyAsync(Read(changed), "admin-1");

        Assert.Equal((3, 0, 0, 0), (first.New, first.Updated, first.Restored, first.Unchanged));
        Assert.Equal((0, 3), (again.New, again.Unchanged));
        Assert.Equal((0, 2, 1, 0), (third.New, third.Updated, third.Restored, third.Unchanged));
        Assert.Equal(4, await _db.LdpArchiveRows.CountAsync());
        Assert.Equal("PT Kurnia", (await _db.LdpArchiveRows.FirstAsync(row => row.JobName == "Standby Agustus")).VendorName);
        Assert.Equal(999_999m, (await _db.LdpArchiveRows.FirstAsync(row => row.SourceRow == 8)).NilaiRealisasi);
        Assert.True(await _db.LdpArchiveRows.AnyAsync(row => row.LdpArchiveRowId == manual.LdpArchiveRowId));
    }

    [Fact]
    public void Export_keeps_dates_and_numbers_typed_in_excel()
    {
        var bytes = new ExcelExporter().Export(
            "Arsip LDP",
            ["Status", "Tgl. Mulai", "Nilai P&L"],
            [["Completed", new DateTime(2025, 7, 1), 1_250_000m]]
        );

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Arsip LDP");
        Assert.Equal("Tgl. Mulai", sheet.Cell(1, 2).GetString());
        Assert.Equal(new DateTime(2025, 7, 1), sheet.Cell(2, 2).GetDateTime());
        Assert.Equal(1_250_000d, sheet.Cell(2, 3).GetDouble());
    }

    [Fact]
    public void An_edit_writes_typed_columns_and_the_detail_values_together()
    {
        var row = Read(Done).Rows.Single();

        LdpArchiveEditor.Apply(
            row,
            "Completed",
            new Dictionary<string, string?> { ["JobName"] = "Angkut rig baru", ["NilaiPnl"] = "2000000", ["StartDate"] = "2025-04-02", ["PicOps"] = "" },
            new Dictionary<string, string?> { ["No."] = "7" }
        );
        var cells = LdpArchiveEditor.Read(row.ValuesJson).ToDictionary(cell => cell.Label);

        Assert.Equal("Angkut rig baru", row.JobName);
        Assert.Equal(2_000_000m, row.NilaiPnl);
        Assert.Equal(new DateTime(2025, 4, 2), row.StartDate);
        Assert.Null(row.PicOps);
        Assert.Equal("Angkut rig baru", cells["Nama Pekerjaan"].Value!.ToString());
        Assert.Equal("number", cells["No."].Type);
        Assert.False(cells.ContainsKey("Pic Ops"));
        Assert.False(LdpArchiveEditor.IsValid("number", "dua juta"));
        Assert.False(LdpArchiveEditor.IsValid("date", "kemarin"));
    }

    [Fact]
    public async Task A_deleted_archive_row_can_be_restored_or_purged_from_deleted_records()
    {
        var kept = new LdpArchiveRow { ImportBatch = "Manual", JobName = "Dipulihkan" };
        var purged = new LdpArchiveRow { ImportBatch = "Manual", JobName = "Dihapus selamanya" };
        _db.LdpArchiveRows.AddRange(kept, purged);
        await _db.SaveChangesAsync();
        _db.LdpArchiveRows.RemoveRange(kept, purged);
        await _db.SaveChangesAsync();

        var deletedRecords = new DeletedRecordsService(_db);
        var listed = await deletedRecords.ListAsync(DeletedRecordKind.LdpArchive);
        var restore = await deletedRecords.RestoreAsync(DeletedRecordKind.LdpArchive, kept.LdpArchiveRowId);
        _db.Users.Add(new User { Id = "admin-1", UserName = "admin", FirstName = "A", LastName = "" });
        await _db.SaveChangesAsync();
        var purge = await new RecordPurgeService(
            _db,
            new NoStorage(),
            Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions { Bucket = "b" }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordPurgeService>.Instance
        ).PurgeAsync(new PurgeRequest(DeletedRecordKind.LdpArchive, purged.LdpArchiveRowId, "Data percobaan arsip", "admin-1", null));

        Assert.Equal(2, listed.Count);
        Assert.True(restore.Succeeded);
        Assert.True(purge.Succeeded, purge.Message);
        Assert.Equal(["Dipulihkan"], await _db.LdpArchiveRows.Select(row => row.JobName).ToListAsync());
        Assert.False(await _db.LdpArchiveRows.IgnoreQueryFilters().AnyAsync(row => row.JobName == "Dihapus selamanya"));
    }

    private sealed class NoStorage : IObjectStorage
    {
        public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default) => Task.CompletedTask;
        public Task UploadAsync(string bucket, string objectKey, Stream content, long size, string contentType, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> GetPresignedUrlAsync(string bucket, string objectKey, TimeSpan expiry, CancellationToken ct = default) => Task.FromResult("");
        public Task<string> GetPresignedUrlHeaderAsync(string bucket, string objectKey, TimeSpan expiry, IDictionary<string, string>? responseHeaders, CancellationToken ct = default) => Task.FromResult("");
    }
}
