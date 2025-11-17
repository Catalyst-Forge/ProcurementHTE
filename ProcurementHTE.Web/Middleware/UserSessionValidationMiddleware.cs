using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Constants;

namespace ProcurementHTE.Web.Middleware
{
    public class UserSessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserSessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUserSessionRepository sessionRepository,
            SignInManager<User> signInManager
        )
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sessionId = context.Request.Cookies[CookieNames.Session];
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId))
                {
                    await SignOutAsync(context, signInManager);
                    return;
                }

                var session = await sessionRepository.GetByIdAsync(sessionId, context.RequestAborted);
                if (session is null || session.UserId != userId || !session.IsActive)
                {
                    await SignOutAsync(context, signInManager);
                    return;
                }

                session.LastAccessedAt = DateTime.UtcNow;
                await sessionRepository.UpdateAsync(session, context.RequestAborted);
                await sessionRepository.SaveAsync(context.RequestAborted);
            }

            await _next(context);
        }

        private static async Task SignOutAsync(HttpContext context, SignInManager<User> signInManager)
        {
            await signInManager.SignOutAsync();
            context.Response.Cookies.Delete(CookieNames.Session);

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Auth/Login");
            }
        }
    }
}
