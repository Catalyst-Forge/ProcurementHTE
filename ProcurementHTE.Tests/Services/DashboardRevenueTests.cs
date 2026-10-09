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

    [Fact]
    public async Task TopVendors_IgnoreOffersFromDeletedProcurements()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        Vendor NewVendor(string code) => new()
        {
            VendorCode = code, VendorName = code, NPWP = "-", Address = "-", City = "-", Province = "-", Email = "v@test",
        };
        var active = NewVendor("VND-ACTIVE");
        var onlyDeleted = NewVendor("VND-DELETED");
        db.Vendors.AddRange(active, onlyDeleted);

        void Offer(Vendor vendor, bool procurementDeleted)
        {
            var procurement = new Procurement { ProcNum = Guid.NewGuid().ToString(), IsDeleted = procurementDeleted };
            var procOffer = new ProcOffer { ProcurementId = procurement.ProcurementId, ItemPenawaran = "Item", Unit = "Unit" };
            var pnl = new ProfitLoss
            {
                ProcurementId = procurement.ProcurementId,
                NoLetterSelectedVendor = "-",
                SelectedVendorId = vendor.VendorId,
            };
            db.AddRange(
                procurement,
                procOffer,
                pnl,
                new VendorOffer
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcOfferId = procOffer.ProcOfferId,
                    VendorId = vendor.VendorId,
                    ProfitLossId = pnl.ProfitLossId,
                    UnitTypeId = "unit-1",
                }
            );
        }

        Offer(active, procurementDeleted: false);
        Offer(onlyDeleted, procurementDeleted: true);
        await db.SaveChangesAsync();

        var top = await new DashboardRepository(db).GetTopVendorsAsync();

        var vendor = Assert.Single(top);
        Assert.Equal("VND-ACTIVE", vendor.VendorCode);
        Assert.Equal(1, vendor.OfferCount);
        Assert.Equal(1, vendor.SelectedCount);
    }
}
