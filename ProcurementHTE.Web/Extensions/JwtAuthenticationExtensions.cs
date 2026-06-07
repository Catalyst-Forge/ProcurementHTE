using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Extensions;

internal static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddDualAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSection = configuration.GetJwtSettingsSection();
        var jwtSettings = configuration.GetRequiredJwtSettings();

        services.Configure<JwtSettings>(jwtSection);

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
                        ShouldUseJwt(context) ? JwtBearerDefaults.AuthenticationScheme
                            : IdentityConstants.ApplicationScheme;
                }
            )
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options => ConfigureJwtBearer(options, jwtSettings)
            );

        return services;
    }

    private static bool ShouldUseJwt(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
            return false;

        var auth = context.Request.Headers.Authorization.ToString();
        return !string.IsNullOrWhiteSpace(auth)
            && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureJwtBearer(
        JwtBearerOptions options,
        JwtSettings jwtSettings
    )
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = true;
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

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }

                return Task.CompletedTask;
            },
            OnMessageReceived = _ => Task.CompletedTask,
            OnTokenValidated = _ => Task.CompletedTask,
            OnChallenge = WriteUnauthorizedResponse,
        };
    }

    private static Task WriteUnauthorizedResponse(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var payload = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                valid = false,
                message = "Token tidak valid atau kedaluwarsa",
                reason = string.IsNullOrEmpty(context.Error) ? "Unauthorized" : context.Error,
                timestamp = DateTime.Now,
            }
        );

        return context.Response.WriteAsync(payload);
    }
}
