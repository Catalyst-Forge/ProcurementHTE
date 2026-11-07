using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class DocumentApprovalRuleSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            try
            {
                // Seed only an empty table. This used to wipe and recreate every rule on
                // each startup, which silently reverted admin edits on every deploy.
                // Deleted rules count as existing so a deliberate delete is respected.
                if (await context.DocumentApprovalRules.IgnoreQueryFilters().AnyAsync())
                    return;

                var targetDocs = new[]
                {
                    "Owner Estimate (OE)",
                    "Rencana Kerja dan Syarat-Syarat (RKS)",
                    "Profit & Loss",
                    "Justifikasi",
                };

                var docTypes = await context
                    .DocumentTypes.AsNoTracking()
                    .Where(dt => targetDocs.Contains(dt.Name))
                    .ToListAsync();

                if (docTypes.Count == 0)
                    return;

                async Task<string?> FindRoleId(string roleName)
                {
                    var role = await roleManager.FindByNameAsync(roleName);
                    return role?.Id;
                }

                // Find users by username for VP level
                async Task<string?> FindUserId(string userName)
                {
                    var user = await context.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserName == userName);
                    return user?.Id;
                }

                var roleManagerTnl = await FindRoleId("Manager Transport & Logistic");
                var roleVp = await FindRoleId("Vice President");

                // User IDs for VP level
                var userKurniawan = await FindUserId("kurniawan"); // Manager TNL
                var userAgus = await FindUserId("agus");           // VP

                // Above the threshold the chain is Manager TNL (Kurniawan) → VP (Agus),
                // because approval stops at the VP at any value.
                var ranges = new (decimal Min, decimal Max, string? SubmitterRole, string? SubmitterUser, string? ApproverRole, string? ApproverUser)[]
                {
                    (ApprovalConstants.CT_THRESHOLD_VP, 99_000_000_000m, roleManagerTnl, userKurniawan, roleVp, userAgus),
                };

                foreach (var docType in docTypes)
                {
                    foreach (var range in ranges)
                    {
                        if (string.IsNullOrWhiteSpace(range.SubmitterRole))
                            continue;

                        // Khusus RKS: batasi ke kategori Services
                        var category =
                            docType.Name.IndexOf("RKS", StringComparison.OrdinalIgnoreCase) >= 0
                                ? ProcurementCategory.Jasa
                                : (ProcurementCategory?)null;

                        context.DocumentApprovalRules.Add(
                            new DocumentApprovalRule
                            {
                                DocumentTypeId = docType.DocumentTypeId,
                                MinAmount = range.Min,
                                MaxAmount = range.Max,
                                SubmitterRoleId = range.SubmitterRole!,
                                SubmitterUserId = range.SubmitterUser,
                                ApproverRoleId = range.ApproverRole,
                                ApproverUserId = range.ApproverUser,
                                ProcurementCategory = category,
                            }
                        );
                    }
                }

                await context.SaveChangesAsync();
            }
            catch
            {
                // Skip jika tabel belum tersedia atau role belum lengkap.
            }
        }
    }
}
