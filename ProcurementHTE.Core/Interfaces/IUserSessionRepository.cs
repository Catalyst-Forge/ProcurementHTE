using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IUserSessionRepository
    {
        Task AddAsync(UserSession session, CancellationToken ct = default);
        Task UpdateAsync(UserSession session, CancellationToken ct = default);
        Task<UserSession?> GetByIdAsync(string sessionId, CancellationToken ct = default);
        Task<IReadOnlyList<UserSession>> GetByUserAsync(string userId, CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);
    }
}
