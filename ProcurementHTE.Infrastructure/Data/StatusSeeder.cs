using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class StatusSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var requiredStatuses = new[]
            {
                "Draft",
                "Created",
                "Waiting Pickup",
                "In Progress",
                "Completed",
                "Closed"
            };

            var existingStatuses = await db.Statuses
                .Select(s => s.StatusName)
                .ToListAsync();

            var missingStatuses = requiredStatuses
                .Where(s => !existingStatuses.Contains(s))
                .Select(s => new Status { StatusName = s })
                .ToList();

            if (missingStatuses.Count > 0)
            {
                await db.Statuses.AddRangeAsync(missingStatuses);
                await db.SaveChangesAsync();
            }
        }
    }
}
