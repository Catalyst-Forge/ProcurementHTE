using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project_25_07.Authorization.Requirements;
using project_25_07.Data;
using project_25_07.Models;

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

      // AUthorization
      services.AddAuthorizationBuilder()
                              // Authorization
                              .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
                              // AUthorization
                              .AddPolicy("ManagementAccess", policy => policy.RequireRole("Admin", "Manager"))
                              // AUthorization
                              .AddPolicy("OperationSite", policy => policy.RequireRole("Admin", "Manager", "AP-PO", "AP-Inv"))
                              // AUthorization
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
