using Microsoft.AspNetCore.Authorization;

namespace ProcurementHTE.Core.Authorization.Requirements {
    public class PermissionRequirement : IAuthorizationRequirement {
        public string Permission { get; }

        public PermissionRequirement(string permission) {
            Permission = permission;
        }
    }
}
