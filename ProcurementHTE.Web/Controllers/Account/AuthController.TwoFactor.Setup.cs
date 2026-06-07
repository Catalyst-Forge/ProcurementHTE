using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [Authorize]
    public async Task<IActionResult> TwoFactorSetup(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (user.TwoFactorEnabled)
            return RedirectToLocal(returnUrl);

        var summary = await _accountService.GetTwoFactorSummaryAsync(
            user.Id,
            HttpContext.RequestAborted
        );
        var emailAvailable = !string.IsNullOrWhiteSpace(user.Email) && user.EmailConfirmed;
        var phoneAvailable = CanUseSmsTwoFactor(user);
        var defaultTab = ResolveTwoFactorSetupTab(
            TempData["TwoFactorSetupActiveTab"] as string,
            emailAvailable,
            phoneAvailable
        );

        var model = new TwoFactorSetupViewModel
        {
            EmailAvailable = emailAvailable,
            PhoneAvailable = phoneAvailable,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            SharedKey = summary.SharedKey,
            AuthenticatorUri = summary.AuthenticatorUri,
            AuthenticatorQrBase64 = summary.AuthenticatorQrBase64,
            ReturnUrl = returnUrl,
            SuccessMessage = TempData["SuccessMessage"] as string,
            ErrorMessage = TempData["ErrorMessage"] as string,
            ActiveTab = defaultTab,
        };

        ViewBag.SetupEmailCooldown = GetCooldownSeconds(TwoFactorEmailCooldownKey);
        ViewBag.SetupSmsCooldown = GetCooldownSeconds(TwoFactorSmsCooldownKey);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactorFromSetup(
        TwoFactorMethod method,
        string verificationCode,
        string? returnUrl = null
    )
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (!ValidateTwoFactorSetupRequest(user, method, verificationCode, returnUrl))
            return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });

        try
        {
            await _accountService.EnableTwoFactorAsync(
                user.Id,
                method,
                verificationCode,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Two-factor authentication berhasil diaktifkan.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] =
                $"Gagal mengaktifkan two-factor authentication: {ex.Message}";
            TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
            return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });
        }

        return RedirectToLocal(returnUrl);
    }

    private bool ValidateTwoFactorSetupRequest(
        ProcurementHTE.Core.Models.User user,
        TwoFactorMethod method,
        string verificationCode,
        string? returnUrl
    )
    {
        _ = returnUrl;
        if (string.IsNullOrWhiteSpace(verificationCode))
            return SetTwoFactorSetupError(method, "Kode verifikasi wajib diisi.");

        if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
            return SetTwoFactorSetupError(method, "Email belum terverifikasi.");

        if (method == TwoFactorMethod.Sms && RequiresPhoneVerification(user))
            return SetTwoFactorSetupError(method, "Nomor HP belum terverifikasi.");

        if (method == TwoFactorMethod.Sms && !CanUseSmsTwoFactor(user))
        {
            var message = IsSmsVerificationAvailable()
                ? "Nomor HP belum siap untuk SMS OTP."
                : "SMS OTP belum dikonfigurasi.";
            return SetTwoFactorSetupError(method, message);
        }

        return true;
    }

    private bool SetTwoFactorSetupError(TwoFactorMethod method, string message)
    {
        TempData["ErrorMessage"] = message;
        TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
        return false;
    }

    private static string ResolveTwoFactorSetupTab(
        string? requestedTab,
        bool emailAvailable,
        bool phoneAvailable
    )
    {
        var defaultTab = string.IsNullOrWhiteSpace(requestedTab)
            ? "auth"
            : requestedTab.ToLowerInvariant();
        if (defaultTab == "email" && !emailAvailable)
            defaultTab = phoneAvailable ? "sms" : "auth";
        if (defaultTab == "sms" && !phoneAvailable)
            defaultTab = emailAvailable ? "email" : "auth";
        return defaultTab;
    }
}
