using Microsoft.AspNetCore.Http;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;
using ProcurementHTE.Web.Constants;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private async Task CompleteInteractiveLoginAsync(
        User user,
        bool rememberMe,
        string? returnUrl
    )
    {
        _ = returnUrl;
        user.LastLoginAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
        await _userManager.UpdateAsync(user);

        await _userActivityNotifier.NotifyUserActivityAsync(
            user.Id,
            user.FullName ?? user.UserName ?? "Unknown",
            isOnline: true
        );

        var userAgent = GetUserAgent();
        var ip = GetRemoteIp();
        var (device, browser) = ClientInfoHelper.Parse(userAgent);
        var session = await _accountService.RegisterSessionAsync(
            user.Id,
            userAgent,
            ip,
            device,
            browser,
            location: null,
            isCurrent: true,
            HttpContext.RequestAborted
        );

        SetSessionCookie(session.UserSessionId, rememberMe);
        await _accountService.LogEventAsync(
            user.Id,
            SecurityLogEventType.LoginSuccess,
            true,
            "Login interaktif berhasil.",
            ip,
            userAgent,
            HttpContext.RequestAborted
        );
    }

    private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();

    private void SetSessionCookie(string sessionId, bool persistent)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        };

        if (persistent)
            options.Expires = DateTimeOffset.UtcNow.AddDays(14);

        Response.Cookies.Append(CookieNames.Session, sessionId, options);
    }

    private void ClearSessionCookie()
    {
        Response.Cookies.Delete(CookieNames.Session);
    }
}
