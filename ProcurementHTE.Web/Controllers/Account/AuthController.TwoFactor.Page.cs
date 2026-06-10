using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna untuk 2FA.");

        var viewModel = new LoginWith2faViewModel
        {
            RememberMe = rememberMe,
            Method = user.TwoFactorMethod,
            ReturnUrl = returnUrl,
        };

        ViewBag.EmailAddress = user.Email;
        ViewBag.PhoneNumber = user.PhoneNumber;
        var recoveryCodes = await _accountService.GetRecoveryCodesSnapshotAsync(
            user.Id,
            HttpContext.RequestAborted
        );
        ViewBag.HasRecoveryCodes = recoveryCodes?.Any() == true;

        var requiresEmailVerification =
            user.TwoFactorMethod == TwoFactorMethod.Email && !user.EmailConfirmed;
        var requiresPhoneVerification =
            user.TwoFactorMethod == TwoFactorMethod.Sms && !user.PhoneNumberConfirmed;

        if (requiresEmailVerification)
        {
            await PreparePendingEmailVerificationAsync(user.Id);
        }
        else if (requiresPhoneVerification)
        {
            await PreparePendingPhoneVerificationAsync(user.Id);
        }
        else if (user.TwoFactorMethod is TwoFactorMethod.Email or TwoFactorMethod.Sms)
        {
            await SendInitialTwoFactorCodeAsync(user.Id, user.TwoFactorMethod);
        }

        ViewBag.PendingEmailCooldown = GetCooldownSeconds(PendingEmailCooldownKey);
        ViewBag.PendingPhoneCooldown = GetCooldownSeconds(PendingPhoneCooldownKey);
        ViewBag.LoginEmailCooldown = GetCooldownSeconds(TwoFactorLoginEmailCooldownKey);
        ViewBag.LoginSmsCooldown = GetCooldownSeconds(TwoFactorLoginSmsCooldownKey);

        ViewData["ReturnUrl"] = returnUrl;
        return View(viewModel);
    }

    private async Task PreparePendingEmailVerificationAsync(string userId)
    {
        ViewBag.RequireEmailVerification = true;
        try
        {
            var callbackUrl = await BuildEmailVerificationCallbackUrlAsync(userId);
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                ViewBag.Auto2faError = "Tidak dapat membuat tautan verifikasi email saat ini.";
                return;
            }

            await _accountService.SendEmailVerificationAsync(
                userId,
                callbackUrl,
                HttpContext.RequestAborted
            );
            if (_emailOptions.UseDevelopmentMode)
                ViewBag.DevMagicLink = callbackUrl;
        }
        catch (Exception ex)
        {
            ViewBag.Auto2faError = $"Gagal mengirim verifikasi email otomatis: {ex.Message}";
        }
    }

    private async Task PreparePendingPhoneVerificationAsync(string userId)
    {
        ViewBag.RequirePhoneVerification = true;
        try
        {
            var verificationCode = await _accountService.GeneratePhoneVerificationCodeAsync(
                userId,
                HttpContext.RequestAborted
            );
            await _accountService.SendPhoneVerificationCodeAsync(
                userId,
                verificationCode,
                HttpContext.RequestAborted
            );
            if (_smsOptions.UseDevelopmentMode)
                ViewBag.DevPhoneOtp = verificationCode;
        }
        catch (Exception ex)
        {
            ViewBag.Auto2faError = $"Gagal mengirim OTP otomatis: {ex.Message}";
        }
    }

    private async Task SendInitialTwoFactorCodeAsync(string userId, TwoFactorMethod method)
    {
        try
        {
            var code = await _accountService.GenerateTwoFactorCodeAsync(
                userId,
                method,
                HttpContext.RequestAborted
            );
            await _accountService.SendTwoFactorCodeAsync(
                userId,
                method,
                code,
                HttpContext.RequestAborted
            );
            ViewBag.Auto2faMessage =
                method == TwoFactorMethod.Email
                    ? "Kode verifikasi dikirim ke email terdaftar Anda."
                    : "Kode verifikasi dikirim via SMS.";

            var devCode = GetDevelopmentTwoFactorCode(method, code);
            if (devCode != null)
                ViewBag.DevTwoFactorCode = devCode;
        }
        catch (Exception ex)
        {
            ViewBag.Auto2faError = $"Gagal mengirim kode verifikasi otomatis: {ex.Message}";
        }

        var loginCooldownKey = method switch
        {
            TwoFactorMethod.Email => TwoFactorLoginEmailCooldownKey,
            TwoFactorMethod.Sms => TwoFactorLoginSmsCooldownKey,
            _ => null,
        };
        if (loginCooldownKey != null)
            StartCooldown(loginCooldownKey);
    }
}
