using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class DataSeeder
    {
        /// <param name="seedSampleData">
        /// Demo users (with known passwords), sample vendors and sample procurements.
        /// Never wanted in production: deleting them there would only bring them back
        /// on the next start.
        /// </param>
        public static async Task SeedAsync(IServiceProvider services, bool seedSampleData)
        {
            var db = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();

            // jalankan tiap seeder (urutan penting)
            await StatusSeeder.SeedAsync(db);
            await RoleSeeder.SeedAsync(roleManager);
            if (seedSampleData)
                await UserSeeder.SeedAsync(userManager);
            await JobTypeSeeder.SeedAsync(db, roleManager);
            await JobTypeMovingMobilizationSeeder.SeedAsync(db, roleManager);
            await JobTypeAngkutanSeeder.SeedAsync(db, roleManager);
            await JobTypeStandBySeeder.SeedAsync(db, roleManager);
            await DocumentApprovalRuleSeeder.SeedAsync(db, roleManager);
            if (seedSampleData)
            {
                await VendorSeeder.SeedAsync(db);
                await ProcurementSeeder.SeedAsync(db);
            }
        }
    }
}
