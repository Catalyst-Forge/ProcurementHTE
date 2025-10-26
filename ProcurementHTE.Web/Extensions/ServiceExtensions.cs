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
using ProcurementHTE.Infrastructure.Storage;

namespace ProcurementHTE.Web.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            // ---------------- DB ----------------
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            // ------------- Identity -------------
            services
                .AddIdentity<User, Role>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 8;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    options.User.RequireUniqueEmail = true;

                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders()
                .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

            // ---------- Authorization Policies ----------
            services
                .AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
                .AddPolicy("ManagementAccess", policy => policy.RequireRole("Admin", "Manager"))
                .AddPolicy("OperationSite", policy => policy.RequireRole("Admin", "Manager", "AP-PO", "AP-Inv"))
                .AddPolicy("MinimumManager", p => p.Requirements.Add(new MinimumRoleRequirement("Manager")))
                .AddPolicy(Permissions.WO.Read, p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Read)))
                .AddPolicy(Permissions.WO.Create, p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Create)))
                .AddPolicy(Permissions.WO.Edit, p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Edit)))
                .AddPolicy(Permissions.WO.Delete, p => p.AddRequirements(new PermissionRequirement(Permissions.WO.Delete)))
                .AddPolicy(Permissions.Vendor.Read, p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Read)))
                .AddPolicy(Permissions.Vendor.Create, p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Create)))
                .AddPolicy(Permissions.Vendor.Edit, p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Edit)))
                .AddPolicy(Permissions.Vendor.Delete, p => p.AddRequirements(new PermissionRequirement(Permissions.Vendor.Delete)))
                .AddPolicy(Permissions.Doc.Read, p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Read)))
                .AddPolicy(Permissions.Doc.Upload, p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Upload)))
                .AddPolicy(Permissions.Doc.Approve, p => p.AddRequirements(new PermissionRequirement(Permissions.Doc.Approve)))
                .AddPolicy("AtLeast.Manager", p => p.AddRequirements(new MinimumRoleRequirement("Manager Transport & Logistic")))
                .AddPolicy(Permissions.Doc.Approve, p => p.AddRequirements(new CanApproveWoDocumentRequirement()));

            // ------------ Cookie & Session ------------
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

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ------------- Options Binding -------------
            // MinIO options (Infrastructure)
            services.Configure<ObjectStorageOptions>(configuration.GetSection("Minio"));


            // ------------- Storage (MinIO) -------------
            // NOTE: Interface yang benar adalah IObjectStorage (bukan IObjectStorageRepository)
            services.AddSingleton<IObjectStorage, MinioStorage>();

            // ------------- Repositories -------------
            services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<ITenderRepository, TenderRepository>();
            services.AddScoped<IWoTypeRepository, WoTypesRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IProfitLossRepository, ProfitLossRepository>();
            services.AddScoped<IVendorOfferRepository, VendorOfferRepository>();
            services.AddScoped<IWoDocumentRepository, WoDocumentRepository>();
            services.AddScoped<IWoTypeDocumentRepository, WoTypeDocumentRepository>();
            services.AddScoped<IWoDocumentApprovalRepository, WoDocumentApprovalRepository>();
            services.AddScoped<IWoDocApprovalFlowRepository, WoDocApprovalFlowRepository>();
            services.AddScoped<IApprovalRepository, ApprovalRepository>();

            // ------------- Services (Core) -------------
            services.AddScoped<IWorkOrderService, WorkOrderService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped<IWoTypeService, WoTypesService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IProfitLossService, ProfitLossService>();
            services.AddScoped<IVendorOfferService, VendorOfferService>();
            services.AddScoped<IWoDocumentService, WoDocumentService>();
            services.AddScoped<IWoTypeDocumentService, WoTypeDocumentService>();
            services.AddScoped<IWoDocumentApprovalService, WoDocumentApprovalService>();
            services.AddScoped<IWoDocApprovalFlowService, WoDocApprovalFlowService>();
            services.AddScoped<IPdfGenerator, PdfGeneratorService>();
            services.AddScoped<IApprovalService, ApprovalService>();

            // ------------- Query Services -------------
            // INI YANG BENAR: WorkOrderDocumentQuery di-bind ke IWorkOrderDocumentQuery (bukan ke IWoDocumentRepository)
            services.AddScoped<IWorkOrderDocumentQuery, WorkOrderDocumentQuery>();

            // ------------- Authorization Handlers -------------
            services.AddScoped<IAuthorizationHandler, MinimumRoleHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationRequirement, CanApproveWoDocumentRequirement>();
            services.AddScoped<IAuthorizationHandler, CanApproveWoDocumentHandler>();

            return services;
        }
    }
}
