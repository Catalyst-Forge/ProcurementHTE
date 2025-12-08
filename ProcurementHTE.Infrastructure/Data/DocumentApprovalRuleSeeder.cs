using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

                var roleAssistant = await FindRoleId("Assistant Manager HTE");
                var roleManagerTnl = await FindRoleId("Manager Transport & Logistic");
                var roleVp = await FindRoleId("Vice President");
                var roleOpDir = await FindRoleId("Operation Director");
                var rolePresDir = await FindRoleId("President Director");
                var roleBoard =
                    await FindRoleId("Dewan Direksi") ?? await FindRoleId("Dewan Komisaris");

                var ranges = new (decimal Min, decimal Max, string? Submitter, string? Approver)[]
                {
                    (500_000_000m, 5_000_000_000m, roleManagerTnl, roleVp),
                    (5_000_000_000m, 10_000_000_000m, roleVp, roleOpDir),
                    (10_000_000_000m, 15_000_000_000m, roleOpDir, rolePresDir),
                    (15_000_000_000m, decimal.MaxValue, rolePresDir, roleBoard ?? rolePresDir),
                };

                foreach (var docType in docTypes)
                {
                    foreach (var range in ranges)
                    {
                        if (string.IsNullOrWhiteSpace(range.Submitter))
                            continue;

                        bool exists = await context.DocumentApprovalRules.AnyAsync(r =>
                            r.DocumentTypeId == docType.DocumentTypeId
                            && r.MinAmount == range.Min
                            && r.MaxAmount == range.Max
                        );

                        if (exists)
                            continue;

                        // Khusus RKS: batasi ke kategori Services
                        var category =
                            docType.Name.IndexOf("RKS", StringComparison.OrdinalIgnoreCase) >= 0
                                ? ProcurementCategory.Services
                                : (ProcurementCategory?)null;

                        context.DocumentApprovalRules.Add(
                            new DocumentApprovalRule
                            {
                                DocumentTypeId = docType.DocumentTypeId,
                                MinAmount = range.Min,
                                MaxAmount = range.Max,
                                SubmitterRoleId = range.Submitter!,
                                ApproverRoleId = range.Approver,
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
