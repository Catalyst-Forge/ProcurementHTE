using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public CustomUserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IOptions<IdentityOptions> optionsAccessor
        )
            : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var roleName in roles)
            {
                if (!identity.HasClaim(Options.ClaimsIdentity.RoleClaimType, roleName))
                {
                    identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName));
                }

                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var claim in roleClaims)
                    {
                        if (!identity.HasClaim(claim.Type, claim.Value))
                        {
                            identity.AddClaim(claim);
                        }
                    }
                }
            }

            return identity;
        }
    }
}
