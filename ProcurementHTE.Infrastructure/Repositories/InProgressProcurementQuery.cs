using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public sealed class InProgressProcurementQuery(AppDbContext db) : IInProgressProcurementQuery
{
    public async Task<InProgressProcurementUsage> GetUsageAsync(CancellationToken ct = default)
    {
        var rows = await db.Procurements
            .AsNoTracking()
            .Where(p => p.ProcurementStatus != ProcurementStatus.DonePO && p.ProcurementStatus != ProcurementStatus.Rejected)
            .GroupBy(p => new { p.JobTypeId, p.ProcurementCategory })
            .Select(g => new { g.Key.JobTypeId, g.Key.ProcurementCategory, Count = g.Count() })
            .ToListAsync(ct);

        return new InProgressProcurementUsage(
            rows.Select(r => (r.JobTypeId, r.ProcurementCategory, r.Count)).ToList()
        );
    }
}
