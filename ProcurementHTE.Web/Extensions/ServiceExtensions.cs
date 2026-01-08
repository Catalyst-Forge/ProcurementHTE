using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Authorization.Handlers;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Services;

namespace ProcurementHTE.Web.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        return services.AddInfrastructure(configuration);
    }

    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // ------------- Services (Core/Application) -------------
        services.AddScoped<IProcurementService, ProcurementService>();
        services.AddScoped<IPurchaseRequisitionService, PurchaseRequisitionService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IJobTypeService, JobTypesService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IProfitLossService, ProfitLossService>();
        services.AddScoped<IVendorOfferService, VendorOfferService>();
        services.AddScoped<IProcDocumentService, ProcDocumentService>();
        services.AddScoped<IJobTypeDocumentService, JobTypeDocumentService>();
        services.AddScoped<IJobTypeDocumentAdminService, JobTypeDocumentAdminService>();
        // Approval per-document removed - approval sekarang hanya di level PR
        // services.AddScoped<IProcDocumentApprovalService, ProcDocumentApprovalService>();
        // services.AddScoped<IProcDocApprovalFlowService, ProcDocApprovalFlowService>();
        services.AddScoped<IDocumentApprovalRuleService, DocumentApprovalRuleService>();
        services.AddScoped<IDocumentApprovalsService, DocumentApprovalsService>();
        // services.AddScoped<IApprovalService, ApprovalService>();
        // services.AddScoped<IApprovalServiceApi, ApprovalServiceApi>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITemplateProvider, FileSystemTemplateProvider>();
        services.AddScoped<IHtmlTokenReplacer, HtmlTokenReplacer>();
        services.AddScoped<IJobTypeCalculationService, JobTypeCalculationService>();
        services.AddScoped<ILdpService, LdpService>();
        services.AddScoped<
            IPurchaseRequisitionTrackingService,
            PurchaseRequisitionTrackingService
        >();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    public static IServiceCollection AddIdentityAndAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
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

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
            options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
            options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
        });

        // ---------- Authorization Policies ----------
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
                p => p.Requirements.Add(new MinimumRoleRequirement("Manager"))
            )
            .AddPolicy(
                Permissions.Procurement.Read,
                p => p.AddRequirements(new PermissionRequirement(Permissions.Procurement.Read))
            )
            .AddPolicy(
                Permissions.Procurement.Create,
                p => p.AddRequirements(new PermissionRequirement(Permissions.Procurement.Create))
            )
            .AddPolicy(
                Permissions.Procurement.Edit,
                p => p.AddRequirements(new PermissionRequirement(Permissions.Procurement.Edit))
            )
            .AddPolicy(
                Permissions.Procurement.Delete,
                p => p.AddRequirements(new PermissionRequirement(Permissions.Procurement.Delete))
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
                p => p.AddRequirements(new MinimumRoleRequirement("Manager Transport & Logistic"))
            )
            .AddPolicy(
                Permissions.Doc.Approve,
                p => p.AddRequirements(new CanApproveProcDocumentRequirement())
            );

        // ------------ Cookie & Session Authentication ------------
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings == null)
        {
            throw new InvalidOperationException("JwtSettings section is missing in configuration");
        }

        if (string.IsNullOrEmpty(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                "JWT Secret is not configured. "
                    + "Please set JwtSettings:Secret in appsettings.json or use User Secrets: "
                    + "dotnet user-secrets set \"JwtSettings:Secret\" \"your-secret-key-here\""
            );
        }

        if (jwtSettings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT Secret must be at least 32 characters long. Current length: {jwtSettings.Secret.Length}"
            );
        }

        if (string.IsNullOrEmpty(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("JWT Issuer is not configured");
        }

        if (string.IsNullOrEmpty(jwtSettings.Audience))
        {
            throw new InvalidOperationException("JWT Audience is not configured");
        }

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "DualAuth";
                options.DefaultAuthenticateScheme = "DualAuth";
                options.DefaultChallengeScheme = "DualAuth";
            })
            .AddPolicyScheme(
                "DualAuth",
                "Select JWT or Cookie",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            var auth = context.Request.Headers.Authorization.ToString();
                            if (
                                !string.IsNullOrEmpty(auth)
                                && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            )
                                return JwtBearerDefaults.AuthenticationScheme;
                        }

                        return IdentityConstants.ApplicationScheme;
                    };
                }
            )
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true; // Set true di production
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.Secret)
                        ),
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role,
                    };

                    // Event handlers untuk debugging (opsional)
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (
                                context.Exception.GetType() == typeof(SecurityTokenExpiredException)
                            )
                            {
                                context.Response.Headers.Append("Token-Expired", "true");
                            }

                            return Task.CompletedTask;
                        },
                        OnMessageReceived = _ => Task.CompletedTask,
                        OnTokenValidated = _ => Task.CompletedTask,
                        OnChallenge = context =>
                        {
                            context.HandleResponse();

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            var payload = System.Text.Json.JsonSerializer.Serialize(
                                new
                                {
                                    valid = false,
                                    message = "Token tidak valid atau kedaluwarsa",
                                    reason = string.IsNullOrEmpty(context.Error)
                                        ? "Unauthorized"
                                        : context.Error,
                                    timestamp = DateTime.Now,
                                }
                            );

                            return context.Response.WriteAsync(payload);
                        },
                    };
                }
            );

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

            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // ------------- Authorization Handlers -------------
        services.AddScoped<IAuthorizationHandler, MinimumRoleHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        // CanApproveProcDocument removed - approval per-document sudah dihapus

        return services;
    }

    public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        return services;
    }
}
