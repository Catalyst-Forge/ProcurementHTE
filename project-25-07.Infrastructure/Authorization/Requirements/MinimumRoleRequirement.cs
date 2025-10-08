using Microsoft.AspNetCore.Authorization;

namespace project_25_07.Infrastructure.Authorization.Requirements {
  public class MinimumRoleRequirement : IAuthorizationRequirement {
    public string Role { get; }
    public MinimumRoleRequirement(string role) {
      Role = role;
    }
  }
}
