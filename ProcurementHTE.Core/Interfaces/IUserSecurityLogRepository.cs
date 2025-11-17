using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IUserSecurityLogRepository
    {
        Task AddAsync(UserSecurityLog log, CancellationToken ct = default);
        Task<IReadOnlyList<UserSecurityLog>> GetRecentAsync(string userId, int take = 20, CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);
    }
}
