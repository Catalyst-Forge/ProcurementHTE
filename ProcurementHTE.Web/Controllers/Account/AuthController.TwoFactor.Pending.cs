using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendPendingEmailVerification(
        bool rememberMe,
        string? returnUrl = null
    )
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        if (user.EmailConfirmed)
        {
            TempData["SuccessMessage"] = "Email sudah terverifikasi.";
            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        if (IsCooldownActive(PendingEmailCooldownKey, out var remaining))
        {
            TempData["ErrorMessage"] =
                $"Tunggu {remaining} detik sebelum mengirim ulang magic link.";
            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        try
        {
            var callbackUrl = await BuildEmailVerificationCallbackUrlAsync(user.Id);
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                TempData["ErrorMessage"] = "Tidak dapat membuat magic link saat ini.";
            }
            else
            {
                await _accountService.SendEmailVerificationAsync(
                    user.Id,
                    callbackUrl,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Magic link verifikasi telah dikirim ulang.";
                if (_emailOptions.UseDevelopmentMode)
                    TempData["TwoFactorFlashDevLink"] = callbackUrl;
                StartCooldown(PendingEmailCooldownKey);
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim ulang magic link: {ex.Message}";
        }

        return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendPendingPhoneVerification(
        bool rememberMe,
        string? returnUrl = null
    )
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        if (user.PhoneNumberConfirmed)
        {
            TempData["SuccessMessage"] = "Nomor HP sudah terverifikasi.";
            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        if (IsCooldownActive(PendingPhoneCooldownKey, out var remaining))
        {
            TempData["ErrorMessage"] = $"Tunggu {remaining} detik sebelum mengirim ulang OTP.";
            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        try
        {
            var verificationCode = await _accountService.GeneratePhoneVerificationCodeAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            await _accountService.SendPhoneVerificationCodeAsync(
                user.Id,
                verificationCode,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "OTP verifikasi SMS telah dikirim ulang.";
            if (_smsOptions.UseDevelopmentMode)
                TempData["TwoFactorFlashDevOtp"] = verificationCode;
            StartCooldown(PendingPhoneCooldownKey);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim ulang OTP SMS: {ex.Message}";
        }

        return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPendingPhone(
        string code,
        bool rememberMe,
        string? returnUrl = null
    )
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Kode OTP harus diisi.";
            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        try
        {
            await _accountService.ConfirmPhoneAsync(user.Id, code, HttpContext.RequestAborted);
            TempData["SuccessMessage"] =
                "Nomor HP berhasil diverifikasi. Silakan lanjutkan login.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memverifikasi OTP: {ex.Message}";
        }

        return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
    }

    private async Task<string?> BuildEmailVerificationCallbackUrlAsync(string userId)
    {
        var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
            userId,
            HttpContext.RequestAborted
        );
        return Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId, token = encodedToken },
            Request.Scheme
        );
    }
}
