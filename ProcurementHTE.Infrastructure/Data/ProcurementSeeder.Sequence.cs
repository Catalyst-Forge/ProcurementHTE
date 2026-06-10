using Microsoft.EntityFrameworkCore;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static async Task<int> GetNextProcurementSequenceAsync(AppDbContext db)
    {
        const string prefix = "PROC";
        var last = await db
            .Procurements.Where(x => x.ProcNum != null && x.ProcNum.StartsWith(prefix))
            .OrderByDescending(x => x.ProcNum)
            .Select(x => x.ProcNum)
            .FirstOrDefaultAsync();

        if (
            !string.IsNullOrWhiteSpace(last)
            && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var current)
        )
            return current + 1;

        return 1;
    }
}
