using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
            return Redirect("~/");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await FindUserAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Akun tidak ditemukan atau tidak aktif");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true
        );

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

        if (result.RequiresTwoFactor)
        {
            if (_securityBypassOptions.BypassTwoFactor)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
                var checkpointRedirect = RedirectIfContactVerificationRequired(user, returnUrl);
                if (checkpointRedirect != null)
                    return checkpointRedirect;
                return RedirectToLocal(returnUrl);
            }

            return RedirectToAction(
                nameof(LoginWith2fa),
                new { returnUrl, rememberMe = model.RememberMe }
            );
        }

        if (result.IsLockedOut)
        {
            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LoginFailed,
                false,
                "Akun terkunci saat login.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );
            ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
        }
        else
        {
            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LoginFailed,
                false,
                "Email atau password salah.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );
            ModelState.AddModelError(string.Empty, "Email atau Password salah");
        }

        return View(model);
    }
}
