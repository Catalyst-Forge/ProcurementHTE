using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class UserSecurityLogRepository : IUserSecurityLogRepository
    {
        private readonly AppDbContext _context;

        public UserSecurityLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserSecurityLog log, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(log);
            await _context.UserSecurityLogs.AddAsync(log, ct);
        }

        public async Task<IReadOnlyList<UserSecurityLog>> GetRecentAsync(
            string userId,
            int take = 20,
            CancellationToken ct = default
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            take = Math.Clamp(take, 1, 100);

            return await _context
                .UserSecurityLogs
                .AsNoTracking()
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task SaveAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    }
}
