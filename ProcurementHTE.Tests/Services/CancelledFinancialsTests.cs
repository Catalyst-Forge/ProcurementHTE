using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class CancelledFinancialsTests
{
    private const decimal LiveRevenue = 100m;
    private const decimal LiveCost = 60m;
    private const decimal LiveProfit = 40m;

    private static async Task<AppDbContext> SeedLiveAndCancelledJobAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var jobType = new JobTypes { TypeName = "Spot" };
        db.Add(jobType);

        void AddJob(ProcurementStatus status, decimal revenue, decimal cost, decimal profit)
        {
            var procurement = new Procurement
            {
                ProcNum = Guid.NewGuid().ToString(),
                ProcurementStatus = status,
                JobTypeId = jobType.JobTypeId,
                JobType = jobType,
            };
            var offer = new ProcOffer
            {
                ProcurementId = procurement.ProcurementId,
                ItemPenawaran = "Item",
                Unit = "Unit",
            };
            var pnl = new ProfitLoss
            {
                ProcurementId = procurement.ProcurementId,
                NoLetterSelectedVendor = "-",
                SelectedVendorId = "vendor-1",
                SelectedVendorFinalOffer = cost,
                Profit = profit,
            };
            db.AddRange(
                procurement,
                offer,
                pnl,
                new ProfitLossItem
                {
                    ProfitLossId = pnl.ProfitLossId,
                    ProcOfferId = offer.ProcOfferId,
                    ItemName = "Item",
                    Revenue = revenue,
                }
            );
        }

        AddJob(ProcurementStatus.DonePO, LiveRevenue, LiveCost, LiveProfit);
        AddJob(ProcurementStatus.Cancelled, 30m, 20m, 10m);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Dashboard_totals_leave_out_cancelled_jobs()
    {
        await using var db = await SeedLiveAndCancelledJobAsync();
        var dashboard = new DashboardRepository(db);

        Assert.Equal(LiveRevenue, await dashboard.GetTotalRevenueAsync());
        Assert.Equal(LiveCost, await dashboard.GetTotalCostAsync());
        Assert.Equal(LiveProfit, await dashboard.GetTotalProfitAsync());
    }

    [Fact]
    public async Task Revenue_by_period_leaves_out_cancelled_jobs()
    {
        await using var db = await SeedLiveAndCancelledJobAsync();
        var profitLoss = new ProfitLossRepository(db);

        Assert.Equal(LiveRevenue, await profitLoss.GetTotalRevenueThisMonthAsync());
        var month = Assert.Single(await profitLoss.GetRevenuePerMonthAsync(DateTime.Now.Year));
        Assert.Equal(LiveRevenue, month.Total);
    }

    [Fact]
    public async Task Breakdowns_still_count_the_job_but_not_its_revenue()
    {
        await using var db = await SeedLiveAndCancelledJobAsync();
        var dashboard = new DashboardRepository(db);

        var region = Assert.Single(await dashboard.GetRegionDistributionAsync());
        Assert.Equal((2, LiveRevenue), (region.Count, region.TotalValue));

        var jobType = Assert.Single(await dashboard.GetJobTypeDistributionAsync());
        Assert.Equal((2, LiveRevenue), (jobType.Count, jobType.TotalValue));

        var trend = Assert.Single(await dashboard.GetMonthlyProcurementTrendAsync());
        Assert.Equal((2, LiveRevenue), (trend.Count, trend.TotalValue));
    }
}
