using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context) => _context = context;

        public Task AddAsync(RefreshToken token, CancellationToken ct = default)
        {
            _context.RefreshTokens.Add(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default) =>
            _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, ct);

        public async Task<bool> HasActiveTokenForDeviceAsync(string userId, string deviceId, CancellationToken ct = default) {
            var now = DateTime.Now;
            return await _context.RefreshTokens
                .AnyAsync(r => r.UserId == userId
                               && r.DeviceId == deviceId
                               && !r.Revoked
                               && r.ExpiresAt > now, ct);
        }


        public Task<int> DeleteByTokenAsync(string token, CancellationToken ct = default) =>
        _context.RefreshTokens.Where(x => x.Token == token).ExecuteDeleteAsync(ct);

        public Task<int> DeleteAllForDeviceAsync(string userId, string? deviceId, CancellationToken ct = default) {
            var q = _context.RefreshTokens.Where(r => r.UserId == userId);
            if (!string.IsNullOrWhiteSpace(deviceId))
                q = q.Where(r => r.DeviceId == deviceId);
            return q.ExecuteDeleteAsync(ct);
        }

        public Task<int> DeleteAllForUserAsync(string userId, CancellationToken ct = default) =>
            _context.RefreshTokens.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);

        public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default) =>
            _context.RefreshTokens.Where(r => r.ExpiresAt <= now).ExecuteDeleteAsync(ct);

        public Task SaveAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    }
}
