using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces {
    public interface IRefreshTokenRepository {
        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default);
        Task<bool> HasActiveTokenForDeviceAsync(string userId, string deviceId, CancellationToken ct = default);
        // Hard deletes:
        Task<int> DeleteByTokenAsync(string token, CancellationToken ct = default);
        Task<int> DeleteAllForDeviceAsync(string userId, string? deviceId, CancellationToken ct = default);
        Task<int> DeleteAllForUserAsync(string userId, CancellationToken ct = default);

        // Kebersihan DB
        Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);
    }
}
