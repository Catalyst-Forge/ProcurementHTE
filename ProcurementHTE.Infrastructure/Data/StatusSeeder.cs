using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class StatusSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (!await db.Statuses.AnyAsync())
            {
                var statuses = new[]
                {
                    new Status { StatusName = "Draft" },
                    new Status { StatusName = "Created" },
                    new Status { StatusName = "In Progress" },
                    new Status { StatusName = "Uploaded" },
                    new Status { StatusName = "Pending" },
                    new Status { StatusName = "Approved" },
                    new Status { StatusName = "Completed" },
                    new Status { StatusName = "Closed" },
                };

                await db.Statuses.AddRangeAsync(statuses);
                await db.SaveChangesAsync();
            }
        }
    }
}
