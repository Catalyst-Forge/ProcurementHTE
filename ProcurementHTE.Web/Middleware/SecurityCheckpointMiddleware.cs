using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Options;

namespace ProcurementHTE.Web.Middleware
{
    public class SecurityCheckpointMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityBypassOptions _bypass;
        private static readonly string[] AllowedPrefixes =
        {
            "/auth/login",
            "/auth/logout",
            "/auth/register",
            "/auth/forgotpassword",
            "/auth/contactverification",
            "/auth/twofactorsetup",
            "/auth/sendcontactemailverification",
            "/auth/sendcontactphoneverification",
            "/auth/verifycontactphone",
            "/auth/resendpendingemailverification",
            "/auth/resendpendingphoneverification",
            "/auth/sendtwofactorsetupcode",
            "/auth/enabletwofactorfromsetup",
            "/auth/verifycontactphone",
            "/auth/loginwith2fa",
            "/auth/loginwithrecoverycode",
        };

        public SecurityCheckpointMiddleware(
            RequestDelegate next,
            IOptions<SecurityBypassOptions> bypassOptions
        )
        {
            _next = next;
            _bypass = bypassOptions.Value ?? new SecurityBypassOptions();
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            if (
                context.User.Identity?.IsAuthenticated == true
                && !IsIgnoredPath(path)
                && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            )
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    if (!_bypass.BypassContactVerification && RequiresContactVerification(user))
                    {
                        Redirect(context, "/Auth/ContactVerification", returnUrl);
                        return;
                    }

                    if (!_bypass.BypassTwoFactor && !user.TwoFactorEnabled)
                    {
                        Redirect(context, "/Auth/TwoFactorSetup", returnUrl);
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static bool IsIgnoredPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            if (path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
                return true;

            if (
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            foreach (var prefix in AllowedPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool RequiresContactVerification(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailConfirmed)
                return true;

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && !user.PhoneNumberConfirmed)
                return true;

            return false;
        }

        private static void Redirect(HttpContext context, string target, string? returnUrl)
        {
            if (context.Response.HasStarted)
                return;

            var encoded = Uri.EscapeDataString(
                string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
            );
            context.Response.Redirect($"{target}?returnUrl={encoded}");
        }
    }
}
