using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<IReadOnlyList<string>> GetRolesAsync(User user);
        Task<User?> GetByIdAsync(string userId, CancellationToken ct = default);
        Task<IList<string>> GetUserIdsByRoleAsync(string roleName, CancellationToken ct = default);
        Task<IList<UserBasicInfo>> GetUsersByRoleAsync(
            string roleName,
            CancellationToken ct = default
        );
    }

    public class UserBasicInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
