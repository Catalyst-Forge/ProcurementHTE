using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Web.Controllers.ApInvoicePickup;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Tests.Services;

public class JobDetailTests
{
    private static LdpArchiveRow ArchiveRow() => new()
    {
        LdpArchiveRowId = "row-1",
        ImportBatch = "test",
        JobName = "Angkut rig",
        VendorName = "PT Kurnia Jaya",
        NoPr = "PR-100",
        PrCreatedAt = new DateTime(2026, 7, 1),
        PoDate = new DateTime(2026, 7, 20),
        ValuesJson = """
            [
              {"label":"PR · Release","type":"date","value":"2026-07-03T00:00:00"},
              {"label":"Aprvl MGR · Selesai","type":"date","value":"2026-07-10T00:00:00"},
              {"label":"Project","type":"text","value":"JWKT"}
            ]
            """,
    };

    [Fact]
    public void Archive_tracking_follows_the_docutrax_stages_with_the_spreadsheet_dates()
    {
        var steps = LdpArchiveTracking.Steps(ArchiveRow());

        Assert.Equal(
            ["PR dibuat", "PR rilis", "Approval Manager", "Approval VP", "Submit INPRO", "Submit Hard Copy", "PO", "Invoice diterima", "SA-SP3"],
            steps.Select(step => step.Label)
        );
        Assert.Equal(new DateTime(2026, 7, 3), steps[1].Date);
        Assert.Equal(new DateTime(2026, 7, 10), steps[2].Date);
        Assert.Equal(new DateTime(2026, 7, 20), steps[6].Date);
        Assert.False(steps[3].Done);
        Assert.Equal(4, steps.Count(step => step.Done));
    }

    [Fact]
    public void The_analyst_and_assistant_manager_stages_show_once_they_have_a_date()
    {
        var row = ArchiveRow();
        LdpArchiveEditor.Apply(row, "Completed", TypedOf(row), new Dictionary<string, string?>(), new Dictionary<string, string?>
        {
            ["Aprvl Analyst · Selesai"] = "2026-07-04",
            ["Aprvl Asst. MGR · Selesai"] = "2026-07-05",
        });

        var steps = LdpArchiveTracking.Steps(row);

        Assert.Equal(
            ["PR dibuat", "PR rilis", "Approval Analyst", "Approval Asst. Manager", "Approval Manager"],
            steps.Take(5).Select(step => step.Label)
        );
        Assert.Equal(new DateTime(2026, 7, 4), steps[2].Date);
        Assert.Equal(new DateTime(2026, 7, 5), steps[3].Date);
    }

    [Fact]
    public void An_admin_edits_the_tracking_dates_apart_from_the_other_columns()
    {
        var row = ArchiveRow();
        var form = LdpArchiveFormModel.From(row);

        Assert.Equal("2026-07-03", form.TrackingDate("PR · Release"));
        Assert.Equal("2026-07-10", form.TrackingDate("Aprvl MGR · Selesai"));
        Assert.Equal(["Project"], form.Others.Select(other => other.Label));

        LdpArchiveEditor.Apply(row, "Completed", TypedOf(row), new Dictionary<string, string?>(), new Dictionary<string, string?>
        {
            ["PR · Release"] = "",
            ["Submit INPRO"] = "2026-07-12",
        });

        var cells = LdpArchiveEditor.Read(row.ValuesJson).ToDictionary(cell => cell.Label);
        Assert.False(cells.ContainsKey("PR · Release"));
        Assert.Equal(("date", "2026-07-12T00:00:00"), (cells["Submit INPRO"].Type, cells["Submit INPRO"].Value!.ToString()));
        Assert.Equal("JWKT", cells["Project"].Value!.ToString());
    }

    private static Dictionary<string, string?> TypedOf(LdpArchiveRow row) => LdpArchiveFormModel.From(row).Typed;

    [Fact]
    public void An_archive_row_without_dates_has_every_stage_open() =>
        Assert.All(LdpArchiveTracking.Steps(new LdpArchiveRow { ImportBatch = "x" }), step => Assert.False(step.Done));

    [Theory]
    [InlineData("ldp", "Ldp", "Kembali ke LDP")]
    [InlineData("ARSIP", "LdpArchive", "Kembali ke Arsip LDP")]
    [InlineData("appo", "ApPoPickup", "Kembali ke AP Purchase Order")]
    [InlineData("apinvoice", "ApInvoicePickup", "Kembali ke AP Invoice")]
    [InlineData("accrual", "Accrual", "Kembali ke Data Accrual")]
    [InlineData("https://evil.example", "Ldp", "Kembali ke LDP")]
    [InlineData(null, "Ldp", "Kembali ke LDP")]
    public void The_back_button_returns_only_to_a_known_list(string? from, string controller, string label)
    {
        var origin = JobDetailOrigin.From(from);

        Assert.Equal(controller, origin.Controller);
        Assert.Equal(label, origin.Label);
    }

    [Fact]
    public void An_archive_detail_falls_back_to_arsip_ldp() =>
        Assert.Same(JobDetailOrigin.Arsip, JobDetailOrigin.From("nope", JobDetailOrigin.Arsip));

    private sealed record NoSort : IListSortProvider
    {
        public ListSort? Current => null;
    }

    private sealed record NoFilters : IListFilterProvider
    {
        public IReadOnlyDictionary<string, string> Filters { get; } = new Dictionary<string, string>();
    }

    [Fact]
    public async Task An_archive_row_opens_as_an_ldp_row_and_a_missing_one_is_not_found()
    {
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.LdpArchiveRows.Add(ArchiveRow());
        await db.SaveChangesAsync();
        var repository = new LdpRepository(db, new NoSort(), new NoFilters());

        var found = await repository.GetArchiveByIdAsync("row-1");

        Assert.NotNull(found);
        Assert.Equal("row-1", found.ArchiveRowId);
        Assert.Equal("Angkut rig", found.TextPekerjaan);
        Assert.Equal("JWKT", found.ProjectRegion);
        Assert.Null(await repository.GetArchiveByIdAsync("missing"));
    }

    [Fact]
    public void The_old_invoice_detail_page_opens_the_shared_detail_page()
    {
        var controller = new ApInvoicePickupController(null!, null!, null!, null!);

        var redirect = Assert.IsType<RedirectToActionResult>(controller.Details("job-1"));

        Assert.Equal("Ldp", redirect.ControllerName);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("job-1", redirect.RouteValues!["id"]);
        Assert.Equal("apinvoice", redirect.RouteValues["from"]);
    }

    [Fact]
    public void Everyone_signed_in_opens_a_job_detail_page()
    {
        foreach (var (controller, action) in new[] { (typeof(ProcurementHTE.Web.Controllers.Ldp.LdpController), "Details"), (typeof(ProcurementHTE.Web.Controllers.Ldp.LdpArchiveController), "Details") })
        {
            var method = controller.GetMethod(action)!;
            Assert.Null(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false).FirstOrDefault());
            Assert.NotEmpty(controller.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false));
        }
    }
}
