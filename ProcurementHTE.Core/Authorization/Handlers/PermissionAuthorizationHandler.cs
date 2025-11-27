using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Authorization.Handlers
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public PermissionAuthorizationHandler(
            UserManager<User> userManager,
            RoleManager<Role> roleManager
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement
        )
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return;

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
                return;

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var claims = await _roleManager.GetClaimsAsync(role);
                    if (
                        claims.Any(c => c.Type == "permission" && c.Value == requirement.Permission)
                    )
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }
        }
    }
}
