using Microsoft.AspNetCore.Authorization;
using ProcurementHTE.Infrastructure.Authorization.Requirements;

namespace ProcurementHTE.Infrastructure.Authorization.Handlers {
  public class MinimumRoleHandler: AuthorizationHandler<MinimumRoleRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement) {
      if (context.User.IsInRole(requirement.Role) || context.User.IsInRole("Admin")) {
        context.Succeed(requirement);
      }

      return Task.CompletedTask;
    }
  }
}
