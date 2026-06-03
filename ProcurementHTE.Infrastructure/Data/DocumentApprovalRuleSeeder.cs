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
                // Clear existing rules first (as requested by user)
                var existingRules = await context.DocumentApprovalRules.ToListAsync();
                if (existingRules.Count > 0)
                {
                    context.DocumentApprovalRules.RemoveRange(existingRules);
                    await context.SaveChangesAsync();
                }

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

                // Find users by username for VP level and above
                async Task<string?> FindUserId(string userName)
                {
                    var user = await context.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserName == userName);
                    return user?.Id;
                }

                var roleManagerTnl = await FindRoleId("Manager Transport & Logistic");
                var roleVp = await FindRoleId("Vice President");
                var roleOpDir = await FindRoleId("Operation Director");
                var rolePresDir = await FindRoleId("President Director");

                // User IDs for VP level and above
                var userKurniawan = await FindUserId("kurniawan"); // Manager TNL
                var userAgus = await FindUserId("agus");           // VP
                var userApriandy = await FindUserId("apriandy");   // Ops Director
                var userFaried = await FindUserId("faried");       // President Director

                // CT Ranges based on ApprovalConstants thresholds (VP level and above):
                // 500M - 5B: Manager TNL (Kurniawan) → VP (Agus)
                // 5B - 10B: VP (Agus) → Ops Director (Apriandy)
                // 10B - 15B: Ops Director (Apriandy) → President Director (Faried)
                // > 15B: Ops Director (Apriandy) → President Director (Faried)
                var ranges = new (decimal Min, decimal Max, string? SubmitterRole, string? SubmitterUser, string? ApproverRole, string? ApproverUser)[]
                {
                    (ApprovalConstants.CT_THRESHOLD_VP, ApprovalConstants.CT_THRESHOLD_OP_DIR, roleManagerTnl, userKurniawan, roleVp, userAgus),
                    (ApprovalConstants.CT_THRESHOLD_OP_DIR, ApprovalConstants.CT_THRESHOLD_PRES_DIR, roleVp, userAgus, roleOpDir, userApriandy),
                    (ApprovalConstants.CT_THRESHOLD_PRES_DIR, 15_000_000_000m, roleOpDir, userApriandy, rolePresDir, userFaried),
                    (15_000_000_000m, 99_000_000_000m, roleOpDir, userApriandy, rolePresDir, userFaried),
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
