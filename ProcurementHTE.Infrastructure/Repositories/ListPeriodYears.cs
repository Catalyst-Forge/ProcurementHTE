using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public sealed class ListPeriodYears(AppDbContext db) : IListPeriodYears
{
    public async Task<IReadOnlyList<int>> ProcurementYearsAsync(CancellationToken ct = default) =>
        await ProcurementYears(db).ToListAsync(ct);

    public async Task<IReadOnlyList<int>> ArchiveYearsAsync(CancellationToken ct = default) =>
        await ArchiveYears(db).ToListAsync(ct);

    // A draft has no start date yet; it stores DateTime.MinValue, which is not a year anyone picks.
    internal static IQueryable<int> ProcurementYears(AppDbContext db) =>
        db.Procurements.AsNoTracking()
            .Where(p => p.StartDate > new DateTime(1900, 1, 1))
            .Select(p => p.StartDate.Year)
            .Distinct()
            .OrderByDescending(year => year);

    internal static IQueryable<int> ArchiveYears(AppDbContext db) =>
        db.LdpArchiveRows.AsNoTracking()
            .Where(row => row.StartDate != null)
            .Select(row => row.StartDate!.Value.Year)
            .Distinct()
            .OrderByDescending(year => year);
}
