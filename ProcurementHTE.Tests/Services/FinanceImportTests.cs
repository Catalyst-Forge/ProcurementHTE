using System.Reflection;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Services;

namespace ProcurementHTE.Tests.Services;

public class FinanceImportTests
{
    private static FinanceFieldSpec Spec(FinanceField field) => FinanceTable.Fields[field];

    private static FinanceTableRow Row(string id, string? sa = null, DateTime? loket = null, bool cancelled = false) => new()
    {
        ProcurementId = id,
        Spmp = "SPMP/0003/2026",
        JobName = "Angkut rig " + id,
        InvoiceNumber = "INV-1",
        SaNo = sa,
        LoketDate = loket,
        IsCancelled = cancelled,
    };

    private static IReadOnlyDictionary<string, FinanceImportJob> Jobs(params FinanceTableRow[] rows) =>
        rows.Select(FinanceImport.FromRow).ToDictionary(job => job.ProcurementId);

    private static ExcelSheet Sheet(string[] headers, params object?[][] rows) =>
        new(headers, rows.Select((cells, i) => new ExcelSheetRow(
            i + 2,
            headers.Zip(cells).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.OrdinalIgnoreCase))).ToList());

    // --- Reading a cell ---

    [Theory]
    [InlineData("2026-11-09")]
    [InlineData("09/11/2026")]
    [InlineData("09 Nov 2026")]
    [InlineData("9 November 2026")]
    public void A_date_reads_from_the_ways_people_type_it(string typed)
    {
        Assert.Null(FinanceImport.Normalize(Spec(FinanceField.LoketDate), typed, out var value));
        Assert.Equal("2026-11-09", value);
    }

    [Fact]
    public void A_date_cell_and_a_date_serial_read_too()
    {
        FinanceImport.Normalize(Spec(FinanceField.LoketDate), new DateTime(2026, 11, 9, 14, 0, 0), out var fromCell);
        FinanceImport.Normalize(Spec(FinanceField.LoketDate), (decimal)new DateTime(2026, 11, 9).ToOADate(), out var fromSerial);

        Assert.Equal("2026-11-09", fromCell);
        Assert.Equal("2026-11-09", fromSerial);
    }

    [Fact]
    public void A_date_that_is_not_one_is_refused_by_name() =>
        Assert.Equal("Tanggal Submit Loket tidak valid: \"kemarin\".", FinanceImport.Normalize(Spec(FinanceField.LoketDate), "kemarin", out _));

    [Theory]
    [InlineData(13000000, "13000000")]
    [InlineData("13.000.000", "13000000")]
    [InlineData("Rp 1.500,5", "1500,5")]
    public void An_amount_reads_from_a_number_cell_or_indonesian_text(object cell, string expected)
    {
        Assert.Null(FinanceImport.Normalize(Spec(FinanceField.NilaiRealisasi), cell is int n ? (decimal)n : cell, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void A_negative_amount_is_refused() =>
        Assert.Equal("Nilai Realisasi tidak boleh negatif.", FinanceImport.Normalize(Spec(FinanceField.NilaiRealisasi), -5m, out _));

    [Fact]
    public void A_long_number_typed_as_a_number_stays_whole()
    {
        FinanceImport.Normalize(Spec(FinanceField.PoNumber), 4500012345m, out var value);
        Assert.Equal("4500012345", value);
    }

    [Fact]
    public void Text_longer_than_the_column_is_refused() =>
        Assert.Equal("No SA maksimal 100 karakter.", FinanceImport.Normalize(Spec(FinanceField.SaNo), new string('x', 101), out _));

    [Fact]
    public void An_empty_cell_is_no_value() =>
        Assert.Null(FinanceImport.Normalize(Spec(FinanceField.SaNo), "  ", out var value) ?? value);

    // --- The plan ---

    private static readonly string[] InvoiceHeaders = [FinanceImport.IdHeader, "SPMP", "No SA", "Submit Loket"];

    [Fact]
    public void Only_values_that_differ_become_changes_and_an_emptied_cell_clears()
    {
        var plan = FinanceImport.Plan(
            FinanceImportTarget.ApInvoice,
            Sheet(InvoiceHeaders,
                ["a", "x", "SA-1", null],
                ["b", "x", "SA-2", new DateTime(2026, 11, 9)],
                ["c", "x", null, null]),
            Jobs(Row("a", sa: "SA-1"), Row("b", sa: "SA-2"), Row("c", sa: "SA-3")));

        Assert.Equal(1, plan.Unchanged);
        Assert.Empty(plan.Rejected);
        Assert.Equal(2, plan.Jobs);
        var loket = Assert.Single(plan.Changes, change => change.ProcurementId == "b");
        Assert.Equal((FinanceField.LoketDate, "-", "09 Nov 2026", "2026-11-09"), (loket.Field, loket.From, loket.To, loket.Value));
        var cleared = Assert.Single(plan.Changes, change => change.ProcurementId == "c");
        Assert.Equal(("SA-3", "dikosongkan", (string?)null), (cleared.From, cleared.To, cleared.Value));
    }

    [Fact]
    public void A_column_the_file_lacks_is_left_alone()
    {
        var plan = FinanceImport.Plan(
            FinanceImportTarget.ApInvoice,
            Sheet([FinanceImport.IdHeader, "No SA"], ["a", "SA-9"]),
            Jobs(Row("a", sa: "SA-1", loket: new DateTime(2026, 11, 1))));

        Assert.Equal(FinanceField.SaNo, Assert.Single(plan.Changes).Field);
    }

    [Fact]
    public void Unknown_repeated_cancelled_and_invalid_rows_are_refused_with_their_reason()
    {
        var plan = FinanceImport.Plan(
            FinanceImportTarget.ApInvoice,
            Sheet(InvoiceHeaders,
                ["zzz", "x", "SA-1", null],
                ["a", "x", "SA-9", null],
                ["a", "x", "SA-8", null],
                ["k", "x", "SA-9", null],
                ["b", "x", "SA-9", "kemarin"],
                [null, "x", "SA-9", null]),
            Jobs(Row("a"), Row("b"), Row("k", cancelled: true)));

        Assert.Equal(["a"], plan.Changes.Select(change => change.ProcurementId).Distinct());
        Assert.Equal(
            [
                (2, "Pekerjaan tidak ada di tabel ini, atau sudah dihapus."),
                (4, "ID Docutrax muncul lebih dari sekali; baris ini dilewati."),
                (5, "Pekerjaan yang sudah di-cancel tidak bisa diubah."),
                (6, "Tanggal Submit Loket tidak valid: \"kemarin\"."),
                (7, "ID Docutrax kosong."),
            ],
            plan.Rejected.Select(row => (row.Row, row.Reason)));
    }

    [Fact]
    public void An_arsip_row_in_an_ldp_file_is_refused_toward_arsip_ldp()
    {
        var plan = FinanceImport.Plan(FinanceImportTarget.Ldp, Sheet([FinanceImport.IdHeader, "No PO"], ["arsip-1", "PO-1"]), Jobs());

        Assert.Equal("Bukan pekerjaan Docutrax. Baris Arsip LDP diperbarui lewat menu Arsip LDP.", Assert.Single(plan.Rejected).Reason);
    }

    [Fact]
    public void A_file_that_is_not_an_export_of_the_list_is_refused_whole()
    {
        var noId = Assert.Throws<FinanceImportException>(() =>
            FinanceImport.Plan(FinanceImportTarget.ApPo, Sheet(["No PO"], ["PO-1"]), Jobs()));
        var noColumn = Assert.Throws<FinanceImportException>(() =>
            FinanceImport.Plan(FinanceImportTarget.Accrual, Sheet([FinanceImport.IdHeader, "No PO"], ["a", "PO-1"]), Jobs(Row("a"))));

        Assert.StartsWith("Kolom ID Docutrax tidak ditemukan", noId.Message);
        Assert.StartsWith("Tidak ada kolom isian yang dikenali", noColumn.Message);
    }

    [Theory]
    [InlineData(FinanceTableKind.ApPo, FinanceImportTarget.ApPo)]
    [InlineData(FinanceTableKind.ApInvoice, FinanceImportTarget.ApInvoice)]
    [InlineData(FinanceTableKind.Accrual, FinanceImportTarget.Accrual)]
    public void An_untouched_export_imports_back_as_no_change(FinanceTableKind kind, FinanceImportTarget target)
    {
        var rows = new[]
        {
            Row("a", sa: "SA-1", loket: new DateTime(2026, 11, 9)) with
            {
                PrNumber = "PR-1", PoNumber = "4500012345", NilaiAccrual = 13_000_000.5m, InproDate = new DateTime(2026, 11, 2), PicAppo = "Diah", NoAccrual = "ACR-1",
            },
            Row("b"),
        };
        var columns = FinanceImport.ExportColumns(kind);
        var bytes = new ExcelExporter().Export("Tes", columns.Select(c => c.Header).ToList(), rows.Select(row => columns.Select(c => c.Value(row)).ToList()));

        var sheet = new ExcelTableReader().Read(new MemoryStream(bytes));
        var plan = FinanceImport.Plan(target, sheet, Jobs(rows));

        Assert.Equal(FinanceImport.IdHeader, sheet.Headers[0]);
        Assert.Empty(plan.Changes);
        Assert.Empty(plan.Rejected);
        Assert.Equal(2, plan.Unchanged);
    }

    [Fact]
    public void A_file_that_is_not_a_workbook_is_refused_with_a_message() =>
        Assert.StartsWith("File tidak bisa dibaca", Assert.Throws<FinanceImportException>(() => new ExcelTableReader().Read(new MemoryStream([1, 2, 3]))).Message);

    // --- Saving ---

    public class TrackingFake : DispatchProxy
    {
        public List<(string Id, FinanceField Field, string? Value)> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(IProcurementTrackingService.UpdateFinanceFieldAsync))
                throw new NotSupportedException(method?.Name);

            var value = (string?)args![2];
            Calls.Add(((string)args[0]!, (FinanceField)args[1]!, value));
            return Task.FromResult(value == "ditolak" ? new FinanceFieldChange(false, "Kolom ditolak.", "") : new FinanceFieldChange(true, null, value ?? "-"));
        }
    }

    [Fact]
    public async Task Each_change_saves_through_the_cell_save_and_a_refusal_is_counted_not_thrown()
    {
        var tracking = DispatchProxy.Create<IProcurementTrackingService, TrackingFake>();
        var plan = new FinanceImportPlan(FinanceImportTarget.ApInvoice,
        [
            new("a", "a", FinanceField.SaNo, "No SA", "-", "SA-1", "SA-1"),
            new("b", "b", FinanceField.SaNo, "No SA", "-", "ditolak", "ditolak"),
            new("c", "c", FinanceField.LoketDate, "Submit Loket", "01 Nov 2026", "dikosongkan", null),
        ], 0, []);

        var result = await new FinanceImportService(null!, null!, tracking).ApplyAsync(plan, "user-1");

        Assert.Equal(2, result.Saved);
        Assert.Equal("Kolom ditolak.", Assert.Single(result.Failed).Reason);
        Assert.Equal([("a", FinanceField.SaNo, "SA-1"), ("b", FinanceField.SaNo, "ditolak"), ("c", FinanceField.LoketDate, (string?)null)], ((TrackingFake)(object)tracking).Calls);
    }

    // --- The LDP export and import agree ---

    [Fact]
    public void The_ldp_export_leads_with_the_id_and_writes_every_column_its_import_reads()
    {
        var headers = ProcurementHTE.Web.Controllers.Ldp.LdpController.ExportColumns.Select(column => column.Header).ToList();

        Assert.Equal(FinanceImport.IdHeader, headers[0]);
        Assert.All(FinanceImport.LdpColumns.Keys, header => Assert.Contains(header, headers));
    }

    [Fact]
    public void An_untouched_ldp_export_imports_back_as_no_change()
    {
        var item = new LdpRecapDto
        {
            ProcurementId = "job-1",
            NoSpmp = "SPMP/0003/2026",
            TextPekerjaan = "Angkut rig",
            NoPr = "PR-1",
            NilaiAccrual = 13_000_000m,
            TanggalSubmitIspa = new DateTime(2026, 11, 2, 9, 30, 0),
            PicApPo = "Diah",
            NoPo = "4500012345",
            TglSubmitLoket = new DateTime(2026, 11, 9),
        };
        var columns = ProcurementHTE.Web.Controllers.Ldp.LdpController.ExportColumns;
        var properties = columns.Select(column => typeof(LdpRecapDto).GetProperty(column.Property)!).ToArray();
        var bytes = new ExcelExporter().Export("LDP", columns.Select(column => column.Header).ToList(), [properties.Select(property => property.GetValue(item)).ToList()]);

        var plan = FinanceImport.Plan(
            FinanceImportTarget.Ldp,
            new ExcelTableReader().Read(new MemoryStream(bytes)),
            new Dictionary<string, FinanceImportJob> { ["job-1"] = FinanceImport.FromLdp(item) });

        Assert.Empty(plan.Changes);
        Assert.Empty(plan.Rejected);
        Assert.Equal(1, plan.Unchanged);
    }

    // --- Who may import ---

    [Theory]
    [InlineData(typeof(ProcurementHTE.Web.Controllers.ApPoPickup.ApPoPickupController), ProcurementHTE.Web.Authorization.ActionRoles.ApPo)]
    [InlineData(typeof(ProcurementHTE.Web.Controllers.ApInvoicePickup.ApInvoicePickupController), ProcurementHTE.Web.Authorization.ActionRoles.ApInvoice)]
    [InlineData(typeof(ProcurementHTE.Web.Controllers.ArPickup.AccrualController), ProcurementHTE.Web.Authorization.ActionRoles.Accrual)]
    [InlineData(typeof(ProcurementHTE.Web.Controllers.Ldp.LdpController), "Admin")]
    public void Only_who_may_fill_the_list_imports_into_it_and_saving_needs_the_antiforgery_token(Type controller, string roles)
    {
        foreach (var name in new[] { "Import", "ImportPreview", "ImportApply" })
        {
            var action = controller.GetMethod(name)!;
            Assert.Equal(roles, action.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()?.Roles);
            if (name != "Import")
                Assert.NotNull(action.GetCustomAttribute<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>());
        }
    }
}

