using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Authorization.Handlers;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Infrastructure.Storage;
using System.Security.Claims;
using System.Text;

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

            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
                options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
                options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role; // ? penting
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
                    p =>
                        p.AddRequirements(
                            new MinimumRoleRequirement("Manager Transport & Logistic")
                        )
                )
                .AddPolicy(
                    Permissions.Doc.Approve,
                    p => p.AddRequirements(new CanApproveProcDocumentRequirement())
                );

            // ------------- Options Binding -------------
            // MinIO options (Infrastructure)
            services.Configure<ObjectStorageOptions>(configuration.GetSection("ObjectStorage"));
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            // ------------ Cookie & Session Authentication ------------
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

            if (jwtSettings == null)
            {
                throw new InvalidOperationException(
                    "JwtSettings section is missing in configuration"
                );
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

            Console.WriteLine($"[JWT Config] Issuer: {jwtSettings.Issuer}");
            Console.WriteLine($"[JWT Config] Audience: {jwtSettings.Audience}");
            Console.WriteLine($"[JWT Config] Secret Length: {jwtSettings.Secret.Length} chars");
            Console.WriteLine(
                $"[JWT Config] Secret Preview: {jwtSettings.Secret.Substring(0, Math.Min(20, jwtSettings.Secret.Length))}..."
            );
            Console.WriteLine(
                $"[JWT Config] Secret Last 10 chars: ...{jwtSettings.Secret.Substring(Math.Max(0, jwtSettings.Secret.Length - 10))}"
            );
            Console.WriteLine(
                $"[JWT Config] Expiration: {jwtSettings.ExpirationInMinutes} minutes"
            );

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
                                    && auth.StartsWith(
                                        "Bearer ",
                                        StringComparison.OrdinalIgnoreCase
                                    )
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
                                var logger = context.HttpContext.RequestServices.GetRequiredService<
                                    ILogger<Program>
                                >();

                                logger.LogError(
                                    "JWT Authentication Failed: {Exception}",
                                    context.Exception.Message
                                );

                                if (
                                    context.Exception.GetType()
                                    == typeof(SecurityTokenExpiredException)
                                )
                                {
                                    context.Response.Headers.Append("Token-Expired", "true");
                                    logger.LogWarning("Token expired");
                                }

                                return Task.CompletedTask;
                            },
                            OnMessageReceived = context =>
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<
                                    ILogger<Program>
                                >();

                                var token = context.Request.Headers.Authorization.ToString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    logger.LogInformation("JWT Token received in header");
                                }
                                else
                                {
                                    logger.LogWarning("No Authorization header found");
                                }
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<
                                    ILogger<Program>
                                >();

                                logger.LogInformation("JWT Token validated successfully");
                                logger.LogInformation(
                                    "User: {User}",
                                    context.Principal?.Identity?.Name
                                );
                                return Task.CompletedTask;
                            },
                            OnChallenge = context =>
                            {
                                context.HandleResponse();

                                var logger = context
                                    .HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("JwtAuth");

                                logger.LogWarning(
                                    "JWT Challenge: {Error} - {Description}",
                                    context.Error,
                                    context.ErrorDescription
                                );

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

            services.AddAuthorization();

            // ------------- Storage (MinIO) -------------
            // NOTE: Interface yang benar adalah IObjectStorage (bukan IObjectStorageRepository)
            services.AddSingleton<IObjectStorage, MinioStorage>();
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            // ------------- Repositories -------------
            services.AddScoped<IProcurementRepository, ProcurementRepository>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<ITenderRepository, TenderRepository>();
            services.AddScoped<IJobTypeRepository, JobTypesRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IProfitLossRepository, ProfitLossRepository>();
            services.AddScoped<IVendorOfferRepository, VendorOfferRepository>();
            services.AddScoped<IProcDocumentRepository, ProcDocumentRepository>();
            services.AddScoped<IJobTypeDocumentRepository, JobTypeDocumentRepository>();
            services.AddScoped<IProcDocumentApprovalRepository, ProcDocumentApprovalRepository>();
            services.AddScoped<IProcDocApprovalFlowRepository, ProcDocApprovalFlowRepository>();
            services.AddScoped<IApprovalRepository, ApprovalRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();

            // ------------- Services (Core) -------------
            services.AddScoped<IProcurementService, ProcurementService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped<IJobTypeService, JobTypesService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IProfitLossService, ProfitLossService>();
            services.AddScoped<IVendorOfferService, VendorOfferService>();
            services.AddScoped<IProcDocumentService, ProcDocumentService>();
            services.AddScoped<IJobTypeDocumentService, JobTypeDocumentService>();
            services.AddScoped<IProcDocumentApprovalService, ProcDocumentApprovalService>();
            services.AddScoped<IProcDocApprovalFlowService, ProcDocApprovalFlowService>();
            services.AddScoped<IPdfGenerator, PdfGeneratorService>();
            services.AddScoped<IApprovalService, ApprovalService>();
            services.AddScoped<IApprovalServiceApi, ApprovalServiceApi>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ITemplateProvider, FileSystemTemplateProvider>();
            services.AddScoped<IHtmlTokenReplacer, HtmlTokenReplacer>();
            services.AddScoped<IDocumentGenerator, DocumentGenerator>();

            // ------------- Query Services -------------
            // INI YANG BENAR: ProcurementDocumentQuery di-bind ke IProcurementDocumentQuery (bukan ke IProcDocumentRepository)
            services.AddScoped<IProcurementDocumentQuery, ProcurementDocumentQuery>();

            // ------------- Authorization Handlers -------------
            services.AddScoped<IAuthorizationHandler, MinimumRoleHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationRequirement, CanApproveProcDocumentRequirement>();
            services.AddScoped<IAuthorizationHandler, CanApproveProcDocumentHandler>();

            // Configure Controllers with JSON options
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
}
