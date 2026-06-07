using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static async Task<ProcurementSeedUsers> LoadSeedUsersAsync(AppDbContext db)
    {
        async Task<User> GetUserOrThrowAsync(string email, string description)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new Exception(
                    $"User '{description}' ({email}) belum ada. Jalankan Role/User seeder dulu."
                );
        }

        return new ProcurementSeedUsers(
            CreatedBy: await GetUserOrThrowAsync("admin@example.com", "Admin"),
            PicOpsUser: await GetUserOrThrowAsync(
                "pro.operation@example.com",
                "PIC Operation"
            ),
            AnalystUser: await GetUserOrThrowAsync("AHte@example.com", "Analyst HTE & LTS"),
            AssistantManagerUser: await GetUserOrThrowAsync(
                "assistantmanagerhte@example.com",
                "Assistant Manager HTE"
            ),
            ManagerUser: await GetUserOrThrowAsync(
                "manager@example.com",
                "Manager Transport & Logistic"
            )
        );
    }

    private static async Task<IReadOnlyDictionary<string, int>> LoadStatusLookupAsync(
        AppDbContext db
    )
    {
        var requiredStatuses = new[] { "Draft", "Created", "In Progress", "Completed" };
        var statuses = await db
            .Statuses.Where(s => s.StatusName != null && requiredStatuses.Contains(s.StatusName))
            .ToListAsync();

        var lookup = statuses.ToDictionary(
            s => s.StatusName!,
            s => s.StatusId,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var statusName in requiredStatuses)
        {
            if (!lookup.ContainsKey(statusName))
                throw new Exception($"Status '{statusName}' belum ada. Jalankan Status seeder dulu.");
        }

        return lookup;
    }

    private static async Task<ProcurementSeedJobTypes> LoadJobTypesAsync(AppDbContext db)
    {
        var angkutan = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Angkutan")
            ?? throw new Exception("JobType 'Angkutan' belum ada.");
        var standBy = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "StandBy")
            ?? throw new Exception("JobType 'StandBy' belum ada.");
        var moving = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving")
            ?? throw new Exception("JobType 'Moving' belum ada.");

        return new ProcurementSeedJobTypes(angkutan, standBy, moving);
    }
}
