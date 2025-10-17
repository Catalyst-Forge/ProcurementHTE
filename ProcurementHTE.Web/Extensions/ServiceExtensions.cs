using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Authorization.Handlers;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Web.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            // Identity
            services
                .AddIdentity<User, Role>(options => {
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
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders()
                .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

            // Authorization Policies
            services
                .AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
                .AddPolicy("ManagementAccess", policy => policy.RequireRole("Admin", "Manager"))
                .AddPolicy(
                    "OperationSite",
                    policy => policy.RequireRole("Admin", "Manager", "AP-PO", "AP-Inv")
                )
                .AddPolicy(
                    "MinimumManager",
                    policy => policy.Requirements.Add(new MinimumRoleRequirement("Manager"))
                )
                .AddPolicy(
                    Permissions.WO.Read,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Read))
                )
                .AddPolicy(
                    Permissions.WO.Create,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Create))
                )
                .AddPolicy(
                    Permissions.WO.Edit,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Edit))
                )
                .AddPolicy(
                    Permissions.WO.Delete,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Delete))
                )
                .AddPolicy(
                    Permissions.Vendor.Read,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Read))
                )
                .AddPolicy(
                    Permissions.Vendor.Create,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Create))
                )
                .AddPolicy(
                    Permissions.Vendor.Edit,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Edit))
                )
                .AddPolicy(
                    Permissions.Vendor.Delete,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Delete))
                )
                .AddPolicy(
                    Permissions.Doc.Read,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Read))
                )
                .AddPolicy(
                    Permissions.Doc.Upload,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Upload))
                )
                .AddPolicy(
                    Permissions.Doc.Approve,
                    p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Approve))
                )
                .AddPolicy(
                    "AtLeast.Manager",
                    p =>
                        p.AddRequirements(
                            new MinimumRoleRequirement("Manager Transport & Logistic")
                        )
                )
                .AddPolicy(Permissions.Doc.Approve, p => p.AddRequirements(new CanApproveWoDocumentRequirement()));

            // Configure Cookie Authentication
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.Name = "ProcurementHTE.Auth";
            });

            // Add Session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register Scopes
            services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
            services.AddScoped<IWorkOrderService, WorkOrderService>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ITenderRepository, TenderRepository>();
            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped<IWoTypeRepository, WoTypesRepository>();
            services.AddScoped<IWoTypeService, WoTypesService>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IProfitLossRepository, ProfitLossRepository>();
            services.AddScoped<IProfitLossService, ProfitLossService>();
            services.AddScoped<IVendorOfferRepository, VendorOfferRepository>();
            services.AddScoped<IVendorOfferService, VendorOfferService>();
            services.AddScoped<IAuthorizationHandler, MinimumRoleHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationRequirement, CanApproveWoDocumentRequirement>();
            services.AddScoped<IAuthorizationHandler, CanApproveWoDocumentHandler>();
            services.AddScoped<IWoDocumentRepository, WoDocumentRepository>();
            services.AddScoped<IWoTypeDocumentRepository, WoTypeDocumentRepository>();
            services.AddScoped<IWoDocumentApprovalRepository, WoDocumentApprovalRepository>();
            services.AddScoped<IWoDocumentRepository, WoDocumentRepository>();
            services.AddScoped<IWoDocumentService, WoDocumentService>();
            services.AddScoped<IWoTypeDocumentRepository, WoTypeDocumentRepository>();
            services.AddScoped<IWoTypeDocumentService, WoTypeDocumentService>();
            services.AddScoped<IWoDocumentApprovalRepository, WoDocumentApprovalRepository>();
            services.AddScoped<IWoDocumentApprovalService, WoDocumentApprovalService>();

            return services;
        }
    }
}
