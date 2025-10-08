using Microsoft.AspNetCore.Authorization;
using project_25_07.Infrastructure.Authorization.Requirements;

namespace project_25_07.Infrastructure.Authorization.Handlers {
  public class MinimumRoleHandler: AuthorizationHandler<MinimumRoleRequirement> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement) {
      if (context.User.IsInRole(requirement.Role) || context.User.IsInRole("Admin")) {
        context.Succeed(requirement);
      }

      return Task.CompletedTask;
    }
  }
}
