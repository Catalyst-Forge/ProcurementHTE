using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<IReadOnlyList<string>> GetRolesAsync(User user);
        Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
    }
}
