using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class DashboardRevenueTests
{
    [Fact]
    public async Task TotalRevenue_LeavesOutDeletedProfitLossesAndProcurements()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        void Add(decimal revenue, bool procurementDeleted = false, bool profitLossDeleted = false)
        {
            var procurement = new Procurement { ProcNum = Guid.NewGuid().ToString(), IsDeleted = procurementDeleted };
            var offer = new ProcOffer { ProcurementId = procurement.ProcurementId, ItemPenawaran = "Item", Unit = "Unit" };
            var pnl = new ProfitLoss
            {
                ProcurementId = procurement.ProcurementId,
                NoLetterSelectedVendor = "-",
                SelectedVendorId = "vendor-1",
                IsDeleted = profitLossDeleted,
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

        Add(100m);
        Add(12m, procurementDeleted: true);
        Add(7m, profitLossDeleted: true);
        await db.SaveChangesAsync();

        Assert.Equal(100m, await new DashboardRepository(db).GetTotalRevenueAsync());
    }
}
