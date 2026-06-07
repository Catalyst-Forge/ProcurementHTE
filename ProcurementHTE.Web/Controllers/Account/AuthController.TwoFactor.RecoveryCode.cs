using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    public async Task<IActionResult> LoginWithRecoveryCode(
        bool? rememberMe = true,
        string? returnUrl = null
    )
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        var model = new LoginWithRecoveryCodeViewModel
        {
            ReturnUrl = returnUrl,
            RememberMe = rememberMe ?? true,
        };
        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(
        LoginWithRecoveryCodeViewModel model,
        string? returnUrl = null
    )
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        var recoveryCode = NormalizeRecoveryCode(model.RecoveryCode);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            HttpContext.Session.SetString(RecoveryResetSessionKey, "1");
            await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
            return RedirectToAction(nameof(RecoveryReset));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
            return View(model);
        }

        await _accountService.LogEventAsync(
            user.Id,
            SecurityLogEventType.LoginFailed,
            false,
            "Recovery code tidak valid.",
            GetRemoteIp(),
            GetUserAgent(),
            HttpContext.RequestAborted
        );
        ModelState.AddModelError(string.Empty, "Recovery code tidak valid.");
        return View(model);
    }
}
