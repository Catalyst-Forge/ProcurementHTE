using Microsoft.AspNetCore.Authorization;

namespace ProcurementHTE.Core.Authorization.Requirements {
  public class MinimumRoleRequirement : IAuthorizationRequirement {
    public string Role { get; }
    public MinimumRoleRequirement(string role) {
      Role = role;
    }
  }
}
