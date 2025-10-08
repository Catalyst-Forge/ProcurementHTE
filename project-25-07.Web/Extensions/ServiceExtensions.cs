using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using project_25_07.Infrastructure.Authorization.Requirements;
using project_25_07.Infrastructure.Data;
using project_25_07.Core.Models;

namespace project_25_07.Extensions {
  public static class ServiceExtensions {
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) {
      // Database
      services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

      // Identity
      services.AddIdentity<User, Role>(options => {
        // Password Settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;

        // Lockout Settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User Settings
        options.User.RequireUniqueEmail = true;

        // Sign In Settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
      }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

      // Authorization Policies
      services.AddAuthorizationBuilder()
        .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
        .AddPolicy("ManagementAccess", policy => policy.RequireRole("Admin", "Manager"))
        .AddPolicy("OperationSite", policy => policy.RequireRole("Admin", "Manager", "AP-PO", "AP-Inv"))
        .AddPolicy("MinimumManager", policy => policy.Requirements.Add(new MinimumRoleRequirement("Manager")));

      // Configure Cookie Authentication
      services.ConfigureApplicationCookie(options => {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
      });

      // Add Session
      services.AddSession(options => {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
      });

      return services;
    }
  }
}
