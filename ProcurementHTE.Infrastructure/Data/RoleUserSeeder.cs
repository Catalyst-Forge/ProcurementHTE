using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class RoleUserSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager, RoleManager<Role> roleManager, AppDbContext db)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
            await SeedStatusesAsync(db);
        }

        private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
        {
            string[] roles =
            {
                "Admin",
                "Manager Transport & Logistic",
                "Analyst HTE & LTS",
                "HTE",
                "Assistant Manager HTE",
                "Vice President",
                "HSE",
                "Supply Chain Management",
            };

            foreach (var roleName in roles)
            {
                // Hindari duplikasi karena beda casing
                if (await roleManager.RoleExistsAsync(roleName)) continue;

                var role = new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = $"{roleName} system role"
                };
                await roleManager.CreateAsync(role);
            }
        }

        private static async Task SeedUsersAsync(UserManager<User> userManager)
        {
            var users = new (string Username, string Email, string Password, string Role)[]
            {
                ("admin", "admin@example.com", "Admin123!", "Admin"),
                ("managerTL", "manager@example.com", "Manager123!", "Manager Transport & Logistic"),
                ("AHte", "AHte@example.com", "AHte123!", "Analyst HTE & LTS"),
                ("hte", "hte@example.com", "Hte1234!", "HTE"),
                ("assistantmanagerhte", "assistantmanagerhte@example.com", "AssistantManager123!", "Assistant Manager HTE"),
                ("vicepresident", "vp@example.com", "VicePresident123!", "Vice President"),
                ("hse", "hse@example.com", "Hse1234!", "HSE"),
                ("scm", "scm@example.com", "Scm1234!", "Supply Chain Management")
            };

            foreach (var u in users)
            {
                var user = await userManager.FindByEmailAsync(u.Email);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = u.Username,
                        NormalizedUserName = u.Username.ToUpperInvariant(),
                        Email = u.Email,
                        NormalizedEmail = u.Email.ToUpperInvariant(),
                        EmailConfirmed = true,
                        FirstName = u.Username,
                        LastName = "Seeder",
                        IsActive = true
                    };

                    var createResult = await userManager.CreateAsync(user, u.Password);
                    if (!createResult.Succeeded)
                        throw new Exception($"Gagal membuat user {u.Email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }

                // Tambah role hanya jika belum ada
                if (!await userManager.IsInRoleAsync(user, u.Role))
                {
                    var addRoleResult = await userManager.AddToRoleAsync(user, u.Role);
                    if (!addRoleResult.Succeeded)
                        throw new Exception($"Gagal add role {u.Role} ke user {u.Email}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static async Task SeedStatusesAsync(AppDbContext db)
        {
            if (await db.Statuses.AnyAsync()) return;

            var statuses = new[]
            {
                new Status { StatusName = "Draft" },
                new Status { StatusName = "Created" },
                new Status { StatusName = "In Progress" },
                new Status { StatusName = "Approved" },
                new Status { StatusName = "Completed" },
                new Status { StatusName = "Closed" },
            };

            await db.Statuses.AddRangeAsync(statuses);
            await db.SaveChangesAsync();
        }

        // Helper publik (opsional) kalau mau dipakai di seeder lain:
        public static async Task<string> GetRoleIdAsync(RoleManager<Role> roleManager, string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) throw new Exception($"Role '{roleName}' belum ada.");
            return role.Id;
        }
    }
}
