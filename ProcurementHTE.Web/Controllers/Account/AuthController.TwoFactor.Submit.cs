using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.ViewModels;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(
        LoginWith2faViewModel model,
        string? returnUrl = null
    )
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        model.Method = user.TwoFactorMethod;
        model.ReturnUrl = returnUrl ?? model.ReturnUrl;
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var code = model.TwoFactorCode.Replace(" ", string.Empty, StringComparison.Ordinal);
        var result = await SignInWithTwoFactorCodeAsync(user.TwoFactorMethod, code, model);

        if (result.Succeeded)
        {
            await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
            var checkpointRedirect = RedirectIfContactVerificationRequired(user, returnUrl);
            if (checkpointRedirect != null)
                return checkpointRedirect;
            var setupRedirect = RedirectIfTwoFactorSetupRequired(user, returnUrl);
            if (setupRedirect != null)
                return setupRedirect;
            return RedirectToLocal(returnUrl);
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
            "Kode 2FA tidak valid.",
            GetRemoteIp(),
            GetUserAgent(),
            HttpContext.RequestAborted
        );
        ModelState.AddModelError(string.Empty, "Kode MFA tidak valid.");
        return View(model);
    }

    private async Task<SignInResult> SignInWithTwoFactorCodeAsync(
        TwoFactorMethod method,
        string code,
        LoginWith2faViewModel model
    )
    {
        if (method == TwoFactorMethod.AuthenticatorApp)
        {
            return await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code,
                model.RememberMe,
                model.RememberMachine
            );
        }

        var provider = method == TwoFactorMethod.Email
            ? TokenOptions.DefaultEmailProvider
            : TokenOptions.DefaultPhoneProvider;

        return await _signInManager.TwoFactorSignInAsync(
            provider,
            code,
            model.RememberMe,
            model.RememberMachine
        );
    }
}
