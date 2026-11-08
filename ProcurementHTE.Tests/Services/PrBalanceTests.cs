using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class PrBalanceTests : IDisposable
{
    private readonly AppDbContext _db = new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options
    );
    private readonly PrBalanceRepository _balances;

    public PrBalanceTests() => _balances = new PrBalanceRepository(_db);

    public void Dispose() => _db.Dispose();

    private PurchaseRequisition Pr(string number, decimal? value = null, bool deleted = false)
    {
        var pr = new PurchaseRequisition
        {
            PrNumber = number,
            RequestDate = DateTime.Today,
            CreatedByUserId = "u-1",
            PrValue = value,
            IsDeleted = deleted,
        };
        _db.PurchaseRequisitions.Add(pr);
        return pr;
    }

    private Procurement Job(PurchaseRequisition? pr, decimal? realization, ProcurementStatus status = ProcurementStatus.DonePO, bool deleted = false)
    {
        var job = new Procurement { ProcNum = "P", JobName = "Job", PrId = pr?.PrId, ProcurementStatus = status, IsDeleted = deleted };
        _db.Procurements.Add(job);
        if (realization is not null)
            _db.ProfitLosses.Add(new ProfitLoss { ProcurementId = job.ProcurementId, NoLetterSelectedVendor = "L", SelectedVendorId = "v-1", RealizationAmount = realization });
        return job;
    }

    [Fact]
    public async Task Used_adds_up_every_job_on_every_record_with_the_same_number()
    {
        var first = Pr("PR-35M", 35_000_000_000m);
        var second = Pr("PR-35M");
        Job(first, 20_000_000m);
        Job(first, 13_000_000m);
        Job(second, 18_000_000m);
        Job(Pr("PR-LAIN"), 99_000_000m);
        await _db.SaveChangesAsync();

        var balance = await _balances.GetAsync("PR-35M");

        Assert.Equal(51_000_000m, balance.Used);
        Assert.Equal(3, balance.Jobs);
        Assert.Equal(35_000_000_000m - 51_000_000m, balance.Remaining);
        Assert.False(balance.IsOverdrawn);
    }

    [Fact]
    public async Task Cancelled_and_deleted_jobs_and_deleted_records_do_not_count()
    {
        var live = Pr("PR-1", 100m);
        Job(live, 40m);
        Job(live, 500m, ProcurementStatus.Cancelled);
        Job(live, 500m, deleted: true);
        Job(Pr("PR-1", deleted: true), 500m);
        await _db.SaveChangesAsync();

        var balance = await _balances.GetAsync("PR-1");

        Assert.Equal(40m, balance.Used);
        Assert.Equal(1, balance.Jobs);
    }

    [Fact]
    public async Task Only_the_latest_profit_loss_of_a_job_counts()
    {
        var job = Job(Pr("PR-1", 100m), null);
        _db.ProfitLosses.AddRange(
            new ProfitLoss { ProcurementId = job.ProcurementId, NoLetterSelectedVendor = "L", SelectedVendorId = "v-1", RealizationAmount = 90m, CreatedAt = DateTime.Now.AddDays(-2) },
            new ProfitLoss { ProcurementId = job.ProcurementId, NoLetterSelectedVendor = "L", SelectedVendorId = "v-1", RealizationAmount = 30m, CreatedAt = DateTime.Now }
        );
        await _db.SaveChangesAsync();

        Assert.Equal(30m, (await _balances.GetAsync("PR-1")).Used);
    }

    [Fact]
    public async Task Setting_the_value_updates_every_record_with_the_number()
    {
        Pr("PR-1");
        Pr("PR-1", 10m);
        Pr("PR-2", 7m);
        await _db.SaveChangesAsync();

        await _balances.SetValueAsync("PR-1", 500m);

        Assert.All(await _db.PurchaseRequisitions.Where(pr => pr.PrNumber == "PR-1").ToListAsync(), pr => Assert.Equal(500m, pr.PrValue));
        Assert.Equal(7m, (await _db.PurchaseRequisitions.SingleAsync(pr => pr.PrNumber == "PR-2")).PrValue);
    }

    [Fact]
    public async Task Going_over_the_value_is_reported_but_not_without_a_value()
    {
        var withValue = Pr("PR-1", 50m);
        Job(withValue, 80m);
        Job(Pr("PR-2"), 80m);
        await _db.SaveChangesAsync();

        var over = await _balances.GetAsync("PR-1");
        var unset = await _balances.GetAsync("PR-2");

        Assert.True(over.IsOverdrawn);
        Assert.Contains("PR-1", over.OverdrawnWarning);
        Assert.Null(unset.Remaining);
        Assert.False(unset.IsOverdrawn);
        Assert.Null(unset.OverdrawnWarning);
    }

    [Fact]
    public async Task A_job_without_a_pr_has_no_balance()
    {
        var job = Job(null, 10m);
        var linked = Job(Pr("PR-9", 100m), 10m);
        await _db.SaveChangesAsync();

        Assert.Null(await _balances.GetForProcurementAsync(job.ProcurementId));
        Assert.Equal("PR-9", (await _balances.GetForProcurementAsync(linked.ProcurementId))!.PrNumber);
    }
}
