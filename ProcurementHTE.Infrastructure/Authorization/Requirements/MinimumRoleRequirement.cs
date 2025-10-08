using Microsoft.AspNetCore.Authorization;

namespace ProcurementHTE.Infrastructure.Authorization.Requirements {
  public class MinimumRoleRequirement : IAuthorizationRequirement {
    public string Role { get; }
    public MinimumRoleRequirement(string role) {
      Role = role;
    }
  }
}
