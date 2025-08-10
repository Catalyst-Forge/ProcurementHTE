using Microsoft.AspNetCore.Identity;
using project_25_07.Models;

namespace project_25_07.Data {
  public class DataSeeder {
    public static async Task SeedAsync(IServiceProvider serviceProvider) {
      var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
      var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();

      string[] roles = ["Admin", "Manager", "AP-PO", "AP-Inv"];

      foreach (var role in roles) {
        if (!await roleManager.RoleExistsAsync(role)) {
          await roleManager.CreateAsync(new Role {
            Name = role,
            Description = GetRoleDescription(role)
          });
        }
      }

      // Seed for Admin Account
      var adminEmail = "admin@example.com";
      var adminUser = await userManager.FindByEmailAsync(adminEmail);

      if (adminUser == null) {
        adminUser = new User {
          UserName = "admin",
          Email = adminEmail,
          FirstName = "System",
          LastName = "Administrator",
          EmailConfirmed = true,
          IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");

        if (result.Succeeded) {
          await userManager.AddToRoleAsync(adminUser, "Admin");
        }
      }

      // Seed for Manager Account
      var managerEmail = "manager@example.com";
      var managerUser = await userManager.FindByEmailAsync(managerEmail);

      if (managerUser == null) {
        managerUser = new User {
          UserName = "manager",
          Email = managerEmail,
          FirstName = "Department",
          LastName = "Manager",
          EmailConfirmed = true,
          IsActive = true,
        };

        var result = await userManager.CreateAsync(managerUser, "Manager123!");

        if (result.Succeeded) {
          await userManager.AddToRoleAsync(managerUser, "Manager");
        }
      }

      // Seed for AP-PO Account
      var appoEmail = "appo@example.com";
      var appoUser = await userManager.FindByEmailAsync(appoEmail);

      if (appoUser == null) {
        appoUser = new User {
          UserName = "appo",
          Email = appoEmail,
          FirstName = "AP-PO",
          EmailConfirmed = true,
          IsActive = true
        };

        var result = await userManager.CreateAsync(appoUser, "Appo123!");

        if (result.Succeeded) {
          await userManager.AddToRoleAsync(appoUser, "AP-PO");
        }
      }

      // Seed for AP-Inv Account
      var appinvEmail = "appinv@example.com";
      var appinvUser = await userManager.FindByEmailAsync(appinvEmail);

      if (appinvUser == null) {
        appinvUser = new User {
          UserName = "appinv",
          Email = appinvEmail,
          FirstName = "AP-Inv",
          EmailConfirmed = true,
          IsActive = true
        };

        var result = await userManager.CreateAsync(appinvUser, "Appinv123!");

        if (result.Succeeded) {
          await userManager.AddToRoleAsync(appinvUser, "AP-Inv");
        }
      }
    }

    private static string GetRoleDescription(string roleName) {
      return roleName switch {
        "Admin" => "Full system access and user managerment",
        "Manager" => "Department management and reporting access",
        "AP-PO" => "Accounts Payable - Purchase Order role",
        "AP-Inv" => "Accounts Payable - Invoice role",
        _ => "Standard role"
      };
    }
  }
}
