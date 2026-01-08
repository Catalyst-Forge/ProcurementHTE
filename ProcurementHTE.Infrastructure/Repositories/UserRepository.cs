using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserRepository(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public Task<User?> GetByIdAsync(string userId, CancellationToken ct = default) =>
            _userManager.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId, ct);

        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            _userManager.Users.FirstOrDefaultAsync(
                u => u.UserName == email || u.Email == email,
                ct
            );

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: true
            );
            return result.Succeeded;
        }

        public async Task<IReadOnlyList<string>> GetRolesAsync(User user) =>
            (await _userManager.GetRolesAsync(user)).ToList();

        public async Task<IList<string>> GetUserIdsByRoleAsync(
            string roleName,
            CancellationToken ct = default
        )
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.Select(u => u.Id).ToList();
        }

        public async Task<IList<UserBasicInfo>> GetUsersByRoleAsync(
            string roleName,
            CancellationToken ct = default
        )
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users
                .Select(u => new UserBasicInfo
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FullName = u.FullName,
                    Email = u.Email,
                })
                .ToList();
        }
    }
}
