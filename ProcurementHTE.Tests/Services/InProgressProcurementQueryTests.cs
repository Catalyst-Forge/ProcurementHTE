using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class InProgressProcurementQueryTests
{
    private static Procurement P(
        string jobTypeId,
        ProcurementCategory category,
        ProcurementStatus status = ProcurementStatus.WaitingApprovalAnalyst,
        bool deleted = false
    ) =>
        new()
        {
            ProcNum = Guid.NewGuid().ToString(),
            JobTypeId = jobTypeId,
            ProcurementCategory = category,
            ProcurementStatus = status,
            IsDeleted = deleted,
        };

    [Fact]
    public async Task Usage_CountsOnlyRunningProcurements_ByTypeAndCategory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        db.Procurements.AddRange(
            P("moving", ProcurementCategory.Jasa),
            P("moving", ProcurementCategory.Jasa, ProcurementStatus.OnSubmitPO),
            P("moving", ProcurementCategory.Barang),
            P("moving", ProcurementCategory.Jasa, ProcurementStatus.DonePO),
            P("moving", ProcurementCategory.Jasa, ProcurementStatus.Rejected),
            P("moving", ProcurementCategory.Jasa, deleted: true),
            P("standby", ProcurementCategory.Jasa)
        );
        await db.SaveChangesAsync();

        var usage = await new InProgressProcurementQuery(db).GetUsageAsync();

        Assert.Equal(2, usage.Count("moving", ProcurementCategory.Jasa));
        Assert.Equal(3, usage.Count("moving", null));
        Assert.Equal(3, usage.Count(null, ProcurementCategory.Jasa));
        Assert.Equal(4, usage.Count(null, null));
        Assert.Equal(0, usage.Count("other", null));
    }
}
