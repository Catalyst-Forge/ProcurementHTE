using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;


namespace ProcurementHTE.Infrastructure.Data
{
    public static class WoTypeSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            // Check if there is any Typename is same
            var existingTypeNames = await context.WoTypes.Select(w => w.TypeName).ToListAsync();

            var types = new List<WoTypes>
            {
                new()
                {
                    TypeName = "StandBy",
                    Description = "WO untuk standby crew/alat dalam periode waktu tertentu",
                },
                new()
                {
                    TypeName = "Moving & Mobilization",
                    Description = "Perpindahan aset/personel dari lokasi A ke B",
                },
                new()
                {
                    TypeName = "SPOT Angkutan",
                    Description = "Order spot untuk kebutuhan pengangkutan cepat",
                },
                new()
                {
                    TypeName = "Other",
                    Description = "Tipe lain di luar kategori yang ada",
                },
            };

            // Filter out types with duplicate TypeName
            var newTypes = types
                .Where(t => !existingTypeNames.Contains(t.TypeName))
                .ToList();

            if (!newTypes.Any())
            {
                return;
            }

            await context.WoTypes.AddRangeAsync(newTypes);
            await context.SaveChangesAsync();
        }
    }
}
