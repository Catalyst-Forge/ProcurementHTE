using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAllSessions()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        await _accountService.DeactivateAllSessionsAsync(user.Id, HttpContext.RequestAborted);
        await _accountService.LogEventAsync(
            user.Id,
            SecurityLogEventType.LogoutAllSessions,
            true,
            "Logout dari semua perangkat.",
            GetRemoteIp(),
            GetUserAgent(),
            HttpContext.RequestAborted
        );

        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        var (device, browser) = ClientInfoHelper.Parse(GetUserAgent());
        var newSession = await _accountService.RegisterSessionAsync(
            user.Id,
            GetUserAgent(),
            GetRemoteIp(),
            device,
            browser,
            null,
            true,
            HttpContext.RequestAborted
        );
        SetSessionCookie(newSession.UserSessionId);

        TempData["SuccessMessage"] = "Semua sesi berhasil di-logout.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return RedirectToAction(nameof(Settings));

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var currentSession = GetCurrentSessionId();
        if (currentSession == sessionId)
        {
            TempData["ErrorMessage"] = "Tidak dapat menghapus sesi yang sedang digunakan.";
            return RedirectToAction(nameof(Settings));
        }

        await _accountService.DeactivateSessionAsync(
            user.Id,
            sessionId,
            HttpContext.RequestAborted
        );
        TempData["SuccessMessage"] = "Sesi berhasil dihentikan.";
        return RedirectToAction(nameof(Settings));
    }
}
