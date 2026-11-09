using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

/// <summary>
/// The range of archived jobs shown in the LDP. The LDP and Arsip LDP read it through
/// <see cref="CurrentAsync"/> and split the archive with <see cref="InRange"/> and
/// <see cref="OutsideRange"/>, which together cover every row exactly once.
/// </summary>
public sealed class LdpArchiveRangeRepository(AppDbContext db) : ILdpArchiveRangeRepository
{
    public Task<LdpArchiveRange> GetAsync(CancellationToken ct = default) => CurrentAsync(db, ct);

    public async Task<IReadOnlyList<LdpArchiveRangeChange>> HistoryAsync(CancellationToken ct = default) =>
        await Newest(db).ToListAsync(ct);

    public async Task AddAsync(LdpArchiveRangeChange change, CancellationToken ct = default)
    {
        var name = await db.Users
            .Where(user => user.Id == change.ChangedByUserId)
            .Select(user => user.FullName ?? user.UserName)
            .FirstOrDefaultAsync(ct);
        change.ChangedByName = Clip(name ?? change.ChangedByUserId, 256);

        db.LdpArchiveRangeChanges.Add(change);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The newest change, or <see cref="LdpArchiveRange.Default"/> when there is none.</summary>
    public static async Task<LdpArchiveRange> CurrentAsync(AppDbContext db, CancellationToken ct = default)
    {
        var newest = await Newest(db)
            .Select(change => new { change.StartFrom, change.StartTo })
            .FirstOrDefaultAsync(ct);
        return newest is null ? LdpArchiveRange.Default : new LdpArchiveRange(newest.StartFrom, newest.StartTo);
    }

    /// <summary>Archived jobs shown in the LDP: started within the range.</summary>
    internal static IQueryable<LdpArchiveRow> InRange(IQueryable<LdpArchiveRow> rows, LdpArchiveRange range)
    {
        var (from, before) = (range.From, range.Before);
        return rows.Where(row => row.StartDate != null && row.StartDate >= from && row.StartDate < before);
    }

    /// <summary>Archived jobs shown in Arsip LDP: no start date, or started outside the range.</summary>
    internal static IQueryable<LdpArchiveRow> OutsideRange(IQueryable<LdpArchiveRow> rows, LdpArchiveRange range)
    {
        var (from, before) = (range.From, range.Before);
        return rows.Where(row => row.StartDate == null || row.StartDate < from || row.StartDate >= before);
    }

    internal static IQueryable<LdpArchiveRangeChange> Newest(AppDbContext db) =>
        db.LdpArchiveRangeChanges.AsNoTracking()
            .OrderByDescending(change => change.ChangedAt)
            .ThenByDescending(change => change.LdpArchiveRangeChangeId);

    private static string Clip(string value, int length) => value.Length <= length ? value : value[..length];
}
