using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();

            // jalankan tiap seeder (urutan penting)
            await RoleUserSeeder.SeedAsync(userManager, roleManager, db);
            await JobTypeSeeder.SeedAsync(db, roleManager);
            await JobTypeMovingMobilizationSeeder.SeedAsync(db, roleManager);
            await VendorSeeder.SeedAsync(db);
            await ProcurementSeeder.SeedAsync(db);
        }
    }
}
