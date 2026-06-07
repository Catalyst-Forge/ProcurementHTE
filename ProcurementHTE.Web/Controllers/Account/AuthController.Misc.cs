using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Web.Constants;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        var sessionId = Request.Cookies[CookieNames.Session];

        if (user?.Id is string userId && !string.IsNullOrWhiteSpace(sessionId))
        {
            await _accountService.DeactivateSessionAsync(
                userId,
                sessionId,
                HttpContext.RequestAborted
            );
            await _accountService.LogEventAsync(
                userId,
                SecurityLogEventType.Logout,
                true,
                "Logout manual dari perangkat.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );
            await _userActivityNotifier.NotifyUserActivityAsync(
                userId,
                user.FullName ?? user.UserName ?? "Unknown",
                isOnline: false
            );
        }

        ClearSessionCookie();
        await _signInManager.SignOutAsync();

        return Redirect("~/");
    }

    public IActionResult AccessDenied()
    {
        return RedirectToAction(
            "Status",
            "Error",
            new { statusCode = StatusCodes.Status403Forbidden }
        );
    }

    public async Task<IActionResult> Refresh()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.RefreshSignInAsync(user!);
        return Redirect("~/");
    }
}
