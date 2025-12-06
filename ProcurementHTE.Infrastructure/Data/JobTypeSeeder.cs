using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class JobTypeSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            if (await context.JobTypes.AnyAsync())
                return;

            // Check if there is any Typename is same
            var existingTypeNames = await context.JobTypes.Select(w => w.TypeName).ToListAsync();

            var types = new List<JobTypes>
            {
                new()
                {
                    TypeName = "StandBy",
                    Description =
                        "Procurement untuk standby crew/alat dalam periode waktu tertentu",
                },
                new()
                {
                    TypeName = "Moving",
                    Description = "Perpindahan aset/personel dari lokasi A ke B",
                },
                new()
                {
                    TypeName = "SPOT Angkutan",
                    Description = "Order spot untuk kebutuhan pengangkutan cepat",
                },
                new() { TypeName = "Other", Description = "Tipe lain di luar kategori yang ada" },
            };

            // Filter out types with duplicate TypeName
            var newTypes = types.Where(t => !existingTypeNames.Contains(t.TypeName)).ToList();

            if (!newTypes.Any())
            {
                return;
            }

            await context.JobTypes.AddRangeAsync(newTypes);
            await context.SaveChangesAsync();
        }
    }
}
