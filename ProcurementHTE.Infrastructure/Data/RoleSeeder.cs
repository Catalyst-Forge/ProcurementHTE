using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(
            RoleManager<Role> roleManager
        )
        {
            string[] roles =
            [
                "Admin",
                "Manager Transport & Logistic",
                "Analyst HTE & LTS",
                "Operation",
                RoleNames.AsstManager,
                "Vice President",
                "AP-PO",
                "AR",
                "AP-Invoice",
                RoleNames.Accrual,
            ];

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        Description = $"{roleName} system role",
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            async Task AddPermissions(string roleName, params string[] permissions)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                var existing = await roleManager.GetClaimsAsync(role!);
                foreach (var permission in permissions.Distinct())
                {
                    if (!existing.Any(c => c.Type == "permission" && c.Value == permission))
                    {
                        await roleManager.AddClaimAsync(role!, new Claim("permission", permission));
                    }
                }
            }

            await AddPermissions(
                "Admin",
                Permissions.Procurement.Read,
                Permissions.Procurement.Create,
                Permissions.Procurement.Edit,
                Permissions.Procurement.Delete,
                Permissions.Vendor.Read,
                Permissions.Vendor.Create,
                Permissions.Vendor.Edit,
                Permissions.Vendor.Delete,
                Permissions.Doc.Read,
                Permissions.Doc.Upload,
                Permissions.Doc.Approve
            );

            await AddPermissions(
                "Vice President",
                Permissions.Procurement.Read,
                Permissions.Vendor.Read,
                Permissions.Doc.Read,
                Permissions.Doc.Approve
            );

            await AddPermissions(
                RoleNames.AsstManager,
                Permissions.Procurement.Read,
                Permissions.Procurement.Create,
                Permissions.Procurement.Edit,
                Permissions.Vendor.Read,
                Permissions.Doc.Read,
                Permissions.Doc.Upload,
                Permissions.Doc.Approve
            );

            await AddPermissions(
                "Manager Transport & Logistic",
                Permissions.Procurement.Read,
                Permissions.Vendor.Read,
                Permissions.Doc.Read,
                Permissions.Doc.Approve
            );

            await AddPermissions(
                "Operation",
                Permissions.Procurement.Read,
                Permissions.Procurement.Create,
                Permissions.Procurement.Edit,
                Permissions.Procurement.Delete,
                Permissions.Doc.Read,
                Permissions.Doc.Upload
            );

            await AddPermissions(
                "AP-PO",
                Permissions.Procurement.Read,
                Permissions.Doc.Read,
                Permissions.Doc.Upload
            );

            await AddPermissions(
                "Analyst HTE & LTS",
                Permissions.Procurement.Read,
                Permissions.Procurement.Create,
                Permissions.Procurement.Edit,
                Permissions.Vendor.Read,
                Permissions.Vendor.Edit,
                Permissions.Doc.Read
            );

            await AddPermissions(
                "AR",
                Permissions.Procurement.Read,
                Permissions.Procurement.Edit,
                Permissions.Doc.Read
            );

            await AddPermissions(
                "AP-Invoice",
                Permissions.Procurement.Read,
                Permissions.Procurement.Edit,
                Permissions.Doc.Read
            );

            await AddPermissions(
                RoleNames.Accrual,
                Permissions.Procurement.Read,
                Permissions.Doc.Read
            );
        }
    }
}
