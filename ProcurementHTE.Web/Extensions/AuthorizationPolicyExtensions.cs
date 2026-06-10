using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Authorization.Requirements;

namespace ProcurementHTE.Web.Extensions;

internal static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services
            .AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("ManagementAccess", policy => policy.RequireRole("Admin", "Manager"))
            .AddPolicy(
                "OperationSite",
                policy => policy.RequireRole("Admin", "Manager", "AP-PO", "AP-Inv")
            )
            .AddPolicy(
                "MinimumManager",
                policy => policy.Requirements.Add(new MinimumRoleRequirement("Manager"))
            )
            .AddPolicy(
                Permissions.Procurement.Read,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Procurement.Read)
                )
            )
            .AddPolicy(
                Permissions.Procurement.Create,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Procurement.Create)
                )
            )
            .AddPolicy(
                Permissions.Procurement.Edit,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Procurement.Edit)
                )
            )
            .AddPolicy(
                Permissions.Procurement.Delete,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Procurement.Delete)
                )
            )
            .AddPolicy(
                Permissions.Vendor.Read,
                policy => policy.AddRequirements(new PermissionRequirement(Permissions.Vendor.Read))
            )
            .AddPolicy(
                Permissions.Vendor.Create,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Vendor.Create)
                )
            )
            .AddPolicy(
                Permissions.Vendor.Edit,
                policy => policy.AddRequirements(new PermissionRequirement(Permissions.Vendor.Edit))
            )
            .AddPolicy(
                Permissions.Vendor.Delete,
                policy => policy.AddRequirements(
                    new PermissionRequirement(Permissions.Vendor.Delete)
                )
            )
            .AddPolicy(
                Permissions.Doc.Read,
                policy => policy.AddRequirements(new PermissionRequirement(Permissions.Doc.Read))
            )
            .AddPolicy(
                Permissions.Doc.Upload,
                policy => policy.AddRequirements(new PermissionRequirement(Permissions.Doc.Upload))
            )
            .AddPolicy(
                "AtLeast.Manager",
                policy => policy.AddRequirements(
                    new MinimumRoleRequirement("Manager Transport & Logistic")
                )
            )
            .AddPolicy(
                Permissions.Doc.Approve,
                policy => policy.AddRequirements(new CanApproveProcDocumentRequirement())
            );

        return services;
    }
}
