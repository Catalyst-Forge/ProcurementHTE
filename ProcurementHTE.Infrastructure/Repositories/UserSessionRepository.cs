using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly AppDbContext _context;

        public UserSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserSession session, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            await _context.UserSessions.AddAsync(session, ct);
        }

        public Task UpdateAsync(UserSession session, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            var tracked = _context.ChangeTracker.Entries<UserSession>()
                .FirstOrDefault(e => e.Entity.UserSessionId == session.UserSessionId);

            if (tracked is not null)
            {
                tracked.CurrentValues.SetValues(session);
            }
            else
            {
                _context.UserSessions.Attach(session);
                _context.Entry(session).State = EntityState.Modified;
            }

            return Task.CompletedTask;
        }

        public Task<UserSession?> GetByIdAsync(string sessionId, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            return _context
                .UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserSessionId == sessionId, ct);
        }

        public async Task<IReadOnlyList<UserSession>> GetByUserAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return await _context
                .UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);
        }

        public Task SaveAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    }
}
