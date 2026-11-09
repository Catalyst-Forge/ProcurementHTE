using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Tests.Services;

public class FinanceTableTests
{
    private static readonly IReadOnlyDictionary<string, string> NoFilters = new Dictionary<string, string>();

    private static Procurement Job(
        ProcurementStatus stage = ProcurementStatus.OnCreateDP3,
        string status = "In Progress",
        bool withPnl = true
    ) =>
        new()
        {
            ProcNum = "PROC000172",
            JobName = "Sewa trailer 40 ft",
            ProcurementStatus = stage,
            Status = new Status { StatusName = status },
            ProfitLosses = withPnl ? [new ProfitLoss { Profit = 37_500_000m, AccrualAmount = 380_000_000m }] : [],
        };

    private static void FillApPo(Procurement p)
    {
        p.PrNumber = "PR-2026-0152";
        p.PoNumber = "4500128734";
        p.IspaSubmitDate = new DateTime(2026, 11, 6);
        p.HardcopySubmittedAt = new DateTime(2026, 11, 7);
        p.PicAppo = "Wulandari";
    }

    [Fact]
    public void ApPo_row_is_done_only_when_all_five_cells_are_filled()
    {
        var job = Job();
        FillApPo(job);
        Assert.True(FinanceTable.ToRow(job).ApPoDone);

        job.PicAppo = " ";
        Assert.False(FinanceTable.ToRow(job).ApPoDone);
    }

    [Fact]
    public void Row_values_come_from_the_latest_profit_and_loss()
    {
        var job = Job();
        job.ProfitLosses =
        [
            new ProfitLoss { Profit = 1m, CreatedAt = new DateTime(2026, 10, 1) },
            new ProfitLoss { Profit = 2m, RealizationAmount = 9m, CreatedAt = new DateTime(2026, 11, 1) },
        ];

        var row = FinanceTable.ToRow(job);

        Assert.Equal(2m, row.NilaiPnl);
        Assert.Equal(9m, row.NilaiRealisasi);
    }

    [Fact]
    public void Apply_trims_text_and_blank_clears_it()
    {
        var job = Job();

        Assert.True(FinanceTable.Apply(job, FinanceField.PicAppo, "  Wulandari  ").Success);
        Assert.Equal("Wulandari", job.PicAppo);

        var cleared = FinanceTable.Apply(job, FinanceField.PicAppo, "   ");
        Assert.True(cleared.Success);
        Assert.Null(job.PicAppo);
        Assert.Equal("-", cleared.Display);
    }

    [Fact]
    public void Apply_rejects_text_longer_than_the_column()
    {
        var job = Job();

        var change = FinanceTable.Apply(job, FinanceField.PrNumber, new string('9', 101));

        Assert.False(change.Success);
        Assert.Null(job.PrNumber);
    }

    [Fact]
    public void Apply_reads_the_date_input_and_rejects_anything_else()
    {
        var job = Job();

        var ok = FinanceTable.Apply(job, FinanceField.HardcopyDate, "2026-11-07");
        Assert.True(ok.Success);
        Assert.Equal(new DateTime(2026, 11, 7), job.HardcopySubmittedAt);
        Assert.Equal("07 Nov 2026", ok.Display);

        Assert.False(FinanceTable.Apply(job, FinanceField.InproDate, "7/11/2026").Success);
        Assert.Null(job.IspaSubmitDate);
    }

    [Fact]
    public void Apply_refuses_a_cancelled_job()
    {
        var job = Job(ProcurementStatus.Cancelled, "Cancelled");

        Assert.False(FinanceTable.Apply(job, FinanceField.SaNo, "SA-1").Success);
        Assert.Null(job.SANo);
    }

    [Fact]
    public void Ap_invoice_cells_save_before_ap_po_has_a_po_number()
    {
        var job = Job(ProcurementStatus.WaitingApprovalManager);

        Assert.True(FinanceTable.Apply(job, FinanceField.InvoiceNumber, "INV-0091").Success);
        Assert.True(FinanceTable.Apply(job, FinanceField.InvoiceDate, "2026-11-10").Success);
        Assert.True(FinanceTable.Apply(job, FinanceField.SaNo, "SA-26-10482").Success);
        Assert.True(FinanceTable.Apply(job, FinanceField.SaDate, "2026-11-11").Success);
        Assert.True(FinanceTable.Apply(job, FinanceField.Sp3No, "SP3/0217").Success);
        Assert.False(FinanceTable.ToRow(job).ApInvoiceDone);

        Assert.True(FinanceTable.Apply(job, FinanceField.Sp3Date, "2026-11-12").Success);
        Assert.True(FinanceTable.Apply(job, FinanceField.LoketDate, "2026-11-13").Success);
        Assert.True(FinanceTable.ToRow(job).ApInvoiceDone);
        Assert.Null(job.PoNumber);
    }

    [Theory]
    [InlineData("1.500.000", 1_500_000)]
    [InlineData("1500000", 1_500_000)]
    [InlineData("Rp 2.750.000,50", 2_750_000.50)]
    public void Ap_po_types_accrual_and_realisasi_the_indonesian_way(string typed, decimal expected)
    {
        var job = Job();

        var change = FinanceTable.Apply(job, FinanceField.NilaiAccrual, typed);

        Assert.True(change.Success);
        Assert.Equal(expected, job.NilaiAccrual);
        Assert.Equal(expected, FinanceTable.ToRow(job).NilaiAccrual);
    }

    [Theory]
    [InlineData("satu juta")]
    [InlineData("1,500,000.00")]
    [InlineData("-5")]
    public void Money_cells_reject_what_is_not_an_amount(string typed)
    {
        var job = Job();

        Assert.False(FinanceTable.Apply(job, FinanceField.NilaiRealisasi, typed).Success);
        Assert.Null(job.NilaiRealisasi);
    }

    [Fact]
    public void Typed_accrual_wins_over_the_old_profit_and_loss_value()
    {
        var job = Job();
        Assert.Equal(380_000_000m, FinanceTable.ToRow(job).NilaiAccrual);

        job.NilaiAccrual = 1m;
        Assert.Equal(1m, FinanceTable.ToRow(job).NilaiAccrual);
    }

    [Fact]
    public void Accrual_table_is_done_when_no_accrual_is_filled()
    {
        var job = Job();
        Assert.False(FinanceTable.ToRow(job).AccrualDone);

        Assert.True(FinanceTable.Apply(job, FinanceField.NoAccrual, "ACR-26-0517").Success);
        Assert.True(FinanceTable.ToRow(job).AccrualDone);
    }

    [Theory]
    [InlineData(ProcurementStatus.OnCreateDP3)]
    [InlineData(ProcurementStatus.WaitingApprovalManager)]
    [InlineData(ProcurementStatus.Cancelled)]
    public void Cells_do_not_move_the_stage_before_approval_is_done(ProcurementStatus stage)
    {
        var job = Job(stage);
        FillApPo(job);

        Assert.Null(FinanceTable.StageAfterApproval(job));
    }

    [Fact]
    public void After_approval_the_first_empty_cell_is_the_current_step()
    {
        var job = Job(ProcurementStatus.OnSubmitISPA);
        Assert.Equal(ProcurementStatus.OnSubmitISPA, FinanceTable.StageAfterApproval(job));

        // Filled out of order: the PO alone does not skip INPRO.
        job.PoNumber = "4500128734";
        Assert.Equal(ProcurementStatus.OnSubmitISPA, FinanceTable.StageAfterApproval(job));

        job.IspaSubmitDate = new DateTime(2026, 11, 6);
        Assert.Equal(ProcurementStatus.OnSubmitHardcopy, FinanceTable.StageAfterApproval(job));

        job.HardcopySubmittedAt = new DateTime(2026, 11, 7);
        Assert.Equal(ProcurementStatus.DonePO, FinanceTable.StageAfterApproval(job));
    }

    [Fact]
    public void Clearing_a_cell_after_done_po_steps_back()
    {
        var job = Job(ProcurementStatus.DonePO);
        FillApPo(job);
        job.PoNumber = null;

        Assert.Equal(ProcurementStatus.OnSubmitPO, FinanceTable.StageAfterApproval(job));
    }

    [Fact]
    public void First_send_goes_to_the_analyst_without_documents_but_needs_a_profit_and_loss()
    {
        var withPnl = FinanceTable.ToRow(Job());
        Assert.True(withPnl.CanSendApproval);
        Assert.Equal("Analyst HTE", withPnl.NextApprovalTarget);
        Assert.False(withPnl.NeedsProfitLossToSend);

        var withoutPnl = FinanceTable.ToRow(Job(withPnl: false));
        Assert.False(withoutPnl.CanSendApproval);
        Assert.True(withoutPnl.NeedsProfitLossToSend);
    }

    [Fact]
    public void After_a_sign_off_ap_po_sends_to_the_next_level()
    {
        var job = Job(ProcurementStatus.WaitingApprovalAsstManager);
        job.ApprovalAwaitingSend = true;

        var waiting = FinanceTable.ToRow(job);
        Assert.Equal("Asst. Manager", waiting.NextApprovalTarget);
        Assert.Null(waiting.WaitingOn);

        job.ApprovalAwaitingSend = false;
        var sent = FinanceTable.ToRow(job);
        Assert.Null(sent.NextApprovalTarget);
        Assert.Equal("Asst. Manager", sent.WaitingOn);
    }

    [Theory]
    [InlineData(ProcurementStatus.OnSubmitISPA)]
    [InlineData(ProcurementStatus.Cancelled)]
    public void Nothing_to_send_after_approvals_or_on_a_cancelled_job(ProcurementStatus stage)
    {
        var job = Job(stage, stage == ProcurementStatus.Cancelled ? "Cancelled" : "In Progress");
        job.ApprovalAwaitingSend = true;

        Assert.Null(FinanceTable.ToRow(job).NextApprovalTarget);
    }

    [Fact]
    public void Approver_cannot_act_until_ap_po_sends_it_to_their_level()
    {
        var tracking = new ProcurementTrackingDto
        {
            CurrentStatus = ProcurementStatus.WaitingApprovalManager,
            ApprovalAwaitingSend = true,
        };
        Assert.False(tracking.IsWaitingApproval);
        Assert.Equal("Manager", tracking.NextApprovalTarget);

        tracking.ApprovalAwaitingSend = false;
        Assert.True(tracking.IsWaitingApproval);
        Assert.Null(tracking.NextApprovalTarget);

        var draft = new ProcurementTrackingDto
        {
            CurrentStatus = ProcurementStatus.OnCreateDP3,
            TotalMandatoryDocs = 6,
            UploadedMandatoryDocs = 0,
        };
        Assert.False(draft.CanSendApproval);
        Assert.Null(draft.NextApprovalTarget);

        draft.PnLSummary = new PnLSummaryCardDto { ProfitLossId = "pnl" };
        Assert.True(draft.CanSendApproval);
        Assert.Equal("Analyst HTE", draft.NextApprovalTarget);
    }

    [Theory]
    [InlineData("Angkutan", ContractType.Spot, true)]
    [InlineData("Angkutan", ContractType.LTC, false)]
    [InlineData("StandBy", ContractType.Spot, false)]
    [InlineData("Moving", ContractType.STB, false)]
    public void Only_spot_angkutan_generates_its_profit_and_loss(string jobType, ContractType contract, bool generates)
    {
        var job = new Procurement { ContractType = contract, JobType = new JobTypes { TypeName = jobType } };

        Assert.Equal(generates, job.GeneratesProfitLossDocument);
    }

    [Theory]
    [InlineData("In Progress", ProcurementStatus.OnCreateDP3, true)]
    [InlineData("Waiting Pickup", ProcurementStatus.OnCreateDP3, true)]
    [InlineData("In Progress", ProcurementStatus.WaitingApprovalAnalyst, false)]
    [InlineData("Created", ProcurementStatus.OnCreateDP3, false)]
    public void Publish_can_be_undone_until_approval_is_sent(string status, ProcurementStatus stage, bool allowed)
    {
        Assert.Equal(allowed, ProcurementService.CanUnpublish(Job(stage, status)));
    }

    private static List<FinanceTableRow> Rows()
    {
        var a = Job();
        a.ProcurementId = "a";
        a.JobName = "Sewa crane 25 ton";
        a.PicAppo = "Wulandari";
        a.PrNumber = "PR-2";

        var b = Job();
        b.ProcurementId = "b";
        b.JobName = "Angkutan BBM solar";
        b.PrNumber = "PR-1";

        var c = Job();
        c.ProcurementId = "c";
        c.JobName = "Pengiriman drill pipe";

        return [FinanceTable.ToRow(a), FinanceTable.ToRow(b), FinanceTable.ToRow(c)];
    }

    [Fact]
    public void Search_looks_across_the_visible_columns()
    {
        var page = FinanceTable.Query(Rows(), "wulan", NoFilters, null, 1, 25);

        Assert.Equal(["a"], page.Items.Select(r => r.ProcurementId));
    }

    [Fact]
    public void Column_filter_keeps_only_matching_rows()
    {
        var filters = new Dictionary<string, string> { ["jobName"] = "solar" };

        var page = FinanceTable.Query(Rows(), null, filters, null, 1, 25);

        Assert.Equal(["b"], page.Items.Select(r => r.ProcurementId));
    }

    [Theory]
    [InlineData(false, new[] { "b", "a", "c" })]
    [InlineData(true, new[] { "a", "b", "c" })]
    public void Sort_puts_empty_cells_last_either_way(bool descending, string[] expected)
    {
        var page = FinanceTable.Query(Rows(), null, NoFilters, new ListSort("prNumber", descending), 1, 25);

        Assert.Equal(expected, page.Items.Select(r => r.ProcurementId));
    }

    [Fact]
    public void A_page_past_the_end_shows_the_last_page()
    {
        var page = FinanceTable.Query(Rows(), null, NoFilters, null, 9, 2);

        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.Total);
        Assert.Single(page.Items);
    }

    [Theory]
    [InlineData("0003/PDC-1110/2026-S0", "0003")]
    [InlineData("4841/PDC-1110/2026-S0", "4841")]
    [InlineData(" 4514 ", "4514")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Tables_show_the_spmp_number_without_its_suffix(string? spmp, string? shown)
    {
        Assert.Equal(shown, FinanceTable.ShortSpmp(spmp));
    }

    [Fact]
    public void Published_statuses_leave_out_draft_and_created()
    {
        Assert.DoesNotContain("Draft", FinanceTable.PublishedStatuses);
        Assert.DoesNotContain("Created", FinanceTable.PublishedStatuses);
        Assert.Contains("In Progress", FinanceTable.PublishedStatuses);
    }
}

public class FinanceTableSqlServerTests
{
    // Constitution II: InMemory does not prove a query translates, so print it with the SQL Server provider.
    [Fact]
    public void Published_jobs_query_translates_for_sql_server()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ProcurementHTE.Infrastructure.Data.AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new ProcurementHTE.Infrastructure.Data.AppDbContext(options);

        var sql = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToQueryString(
            ProcurementHTE.Infrastructure.Repositories.ProcurementRepository.PublishedForFinance(db)
        );

        Assert.Contains("[StatusName]", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void Procurement_column_filters_translate_for_sql_server()
    {
        var options = new DbContextOptionsBuilder<ProcurementHTE.Infrastructure.Data.AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new ProcurementHTE.Infrastructure.Data.AppDbContext(options);
        var filters = new Dictionary<string, string>
        {
            ["spmp"] = "0003",
            ["jobName"] = "trailer",
            ["jobType"] = "Angkut",
            ["region"] = "jw",
            ["vendor"] = "Sinar",
        };

        var sql = ProcurementHTE.Infrastructure.Repositories.ProcurementRepository
            .ApplyColumnFilters(db.Procurements, filters)
            .ToQueryString();

        Assert.Contains("LIKE", sql);
        Assert.Contains("[ProjectRegion]", sql);
        Assert.Contains("[VendorName]", sql);
    }
}

public class ApprovalAwaitingSendQueryTests
{
    [Fact]
    public async Task Pending_approvals_leave_out_jobs_ap_po_has_not_sent_on_yet()
    {
        var options = new DbContextOptionsBuilder<ProcurementHTE.Infrastructure.Data.AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ProcurementHTE.Infrastructure.Data.AppDbContext(options);
        db.Procurements.AddRange(
            new Procurement
            {
                ProcNum = "SENT",
                ProcurementStatus = ProcurementStatus.WaitingApprovalManager,
                ManagerUserId = "manager",
            },
            new Procurement
            {
                ProcNum = "WAITING-FOR-AP-PO",
                ProcurementStatus = ProcurementStatus.WaitingApprovalManager,
                ManagerUserId = "manager",
                ApprovalAwaitingSend = true,
            }
        );
        await db.SaveChangesAsync();
        var repo = new ProcurementHTE.Infrastructure.Repositories.DashboardRepository(db);

        Assert.Equal(1, await repo.GetPendingApprovalCountByUserAsync("manager", ["Manager Transport & Logistic"]));
        Assert.Equal(1, await repo.GetPendingApprovalCountByUserAsync("admin", ["Admin"]));
    }
}

public class SpmpIdentifierTests
{
    [Fact]
    public void People_see_the_spmp_number_not_the_proc_number()
    {
        var job = new Procurement { ProcNum = "PROC000172", SpmpNumber = "0003/PDC-1110/2026-S0", JobName = "Sewa trailer" };

        Assert.Equal("SPMP 0003", job.DisplayId);
        Assert.Equal("SPMP 0003 (Sewa trailer)", job.DisplayName);
        Assert.DoesNotContain("PROC", job.DisplayName);
    }

    [Fact]
    public void A_job_without_spmp_goes_by_its_name()
    {
        var job = new Procurement { ProcNum = "PROC000173", JobName = "Sewa crane" };

        Assert.Equal("Sewa crane", job.DisplayId);
        Assert.Equal("Sewa crane", job.DisplayName);
        Assert.Equal("(tanpa SPMP)", FinanceTable.SpmpTitle(job.SpmpNumber));
    }

    [Fact]
    public void Tracking_heading_uses_the_spmp_number()
    {
        var tracking = new ProcurementTrackingDto { ProcNum = "PROC000172", SpmpNumber = "4841/PDC-1110/2026-S0", JobName = "Angkut" };

        Assert.Equal("SPMP 4841", tracking.DisplayId);
    }
}
