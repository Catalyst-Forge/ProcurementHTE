using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data {
  public class DataSeeder {
    public static async Task SeedAsync(IServiceProvider serviceProvider) {
      var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
      var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
      var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

      // Role Seeds
      await RoleSeed(roleManager);

      // User Seeds
      var users = new (string UserName, string Email, string FirstName, string LastName, string Password, string Role)[] {
        ("admin", "admin@example.com", "System", "Administrator", "Admin123!", "Admin"),
        ("supervisor", "supervisor@example.com", "Supervisor", "", "Supervisor123!", "Supervisor"),
        ("assistantmanager", "assistantmanager@example.com", "Assistant", "Manager", "AssistantManager123!", "Assistant Manager"),
        ("manager", "manager@example.com", "Department", "Manager", "Manager123!", "Manager"),
        ("vicepresident", "vicepresident@example.com", "Vice", "President", "VicePresident123!", "Vice President"),
        ("hsse", "hsse@example.com", "HSSE", "", "Hsse123!", "HSSE"),
        ("hte", "hte@example.com", "HTE", "", "Hte1234!", "HTE")
      };

      foreach (var user in users) {
        await UserSeed(userManager, user.UserName, user.Email, user.FirstName, user.LastName, user.Password, user.Role);
      }

      // Work Order Type Seeds
      await WoTypeSeed(dbContext);

      // Work Order Status Seeds
      await StatusWoSeed(dbContext);
    }

    private static async Task RoleSeed(RoleManager<Role> roleManager) {
      string[] roles = ["Admin", "Manager", "HTE", "Supervisor", "Assistant Manager", "Vice President", "HSSE"];

      foreach (var role in roles) {
        if (!await roleManager.RoleExistsAsync(role)) {
          await roleManager.CreateAsync(new Role {
            Name = role,
            Description = GetRoleDescription(role)
          });
        }
      }
    }

    private static string GetRoleDescription(string roleName) {
      return roleName switch {
        "Admin" => "Full system access and user managerment",
        "Manager" => "Department management and reporting access",
        "HTE" => "Handles technical engineering and maintenance responsibilities",
        "Supervisor" => "Supervises daily operations and ensures team perfomance",
        "Assistant Manager" => "Supports manager in overseeing department tasks and reporting",
        "Vice President" => "Executive-level role responsible for strategic planning and decision making",
        "HSSE" => "Health, Safety, Security, and Environment compliance role",
        _ => "Standard role"
      };
    }

    private static async Task UserSeed(UserManager<User> userManager, string username, string email, string firstName, string lastName, string password, string role) {
      var user = await userManager.FindByEmailAsync(email);

      if (user == null) {
        user = new User {
          UserName = username,
          Email = email,
          FirstName = firstName,
          LastName = lastName,
          EmailConfirmed = true,
          IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded) {
          await userManager.AddToRoleAsync(user, role);
        }
      }
    }

    private static async Task WoTypeSeed(AppDbContext dbContext) {
      if (!await dbContext.WoTypes.AnyAsync()) {
        var types = new List<WoTypes> {
          new() { TypeName = "StandBy" },
          new() { TypeName = "Moving" },
          new() { TypeName = "SPOT Angkutan" }
        };

        await dbContext.WoTypes.AddRangeAsync(types);
        await dbContext.SaveChangesAsync();
      }
    }

    private static async Task StatusWoSeed(AppDbContext dbContext) {
      if (!await dbContext.Statuses.AnyAsync()) {
        var statuses = new List<Status> {
          new() { StatusName = "Draft" },
          new() { StatusName = "Approved" },
          new() { StatusName = "In Progress" },
          new() { StatusName = "On Hold" },
          new() { StatusName = "Completed" },
          new() { StatusName = "Cancelled" },
          new() { StatusName = "Rejected" }
        };

        await dbContext.Statuses.AddRangeAsync(statuses);
        await dbContext.SaveChangesAsync();
      }
    }
  }
}
