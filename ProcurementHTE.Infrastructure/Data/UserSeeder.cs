using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static partial class UserSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager)
        {
            var users = GetUsers();

            foreach (var u in users)
            {
                await UserSeed(
                    userManager,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Password,
                    u.Role
                );
            }
        }

        private static async Task UserSeed(
            UserManager<User> userManager,
            string userName,
            string email,
            string firstName,
            string lastName,
            string password,
            string role
        )
        {
            // Check if user already exists by email OR username
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = await userManager.FindByNameAsync(userName);
            }

            if (user == null)
            {
                user = new User
                {
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    FirstName = firstName,
                    LastName = lastName,
                    JobTitle = role,
                    IsActive = true,
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                    throw new Exception(
                        $"Gagal membuat user {email}: {string.Join(", ", createResult.Errors.Select(err => err.Description))}"
                    );
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                    throw new Exception(
                        $"Gagal menambahkan role {role} ke user {userName}: {string.Join(", ", addRoleResult.Errors.Select(err => err.Description))}"
                    );
            }
        }
    }
}
