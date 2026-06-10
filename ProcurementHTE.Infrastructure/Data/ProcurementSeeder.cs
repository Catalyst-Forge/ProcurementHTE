using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Procurements.AnyAsync())
            return;

        var users = await LoadSeedUsersAsync(db);
        var statuses = await LoadStatusLookupAsync(db);
        var jobTypes = await LoadJobTypesAsync(db);

        var nextSequence = await GetNextProcurementSequenceAsync(db);
        string NextProcNum() => $"PROC{nextSequence++:D6}";

        var procurements = BuildSampleProcurements()
            .Select(seed => BuildProcurement(seed, users, statuses, jobTypes, NextProcNum))
            .ToList();

        db.Procurements.AddRange(procurements);
        await db.SaveChangesAsync();
    }
}
