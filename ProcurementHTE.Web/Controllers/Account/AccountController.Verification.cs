using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailVerification()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            var callbackUrl = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme
            );

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                TempData["ErrorMessage"] = "Tidak dapat membuat tautan verifikasi saat ini.";
                return RedirectToAction(nameof(Settings));
            }

            await _accountService.SendEmailVerificationAsync(
                user.Id,
                callbackUrl,
                HttpContext.RequestAborted
            );

            if (_emailOptions.UseDevelopmentMode)
            {
                TempData["DevMagicLink"] = callbackUrl;
            }
            TempData["SuccessMessage"] = $"Magic link verifikasi telah kami kirim ke {user.Email}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim tautan verifikasi email: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Tautan tidak valid.";
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            await _accountService.ConfirmEmailAsync(userId, token, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Email Anda telah terverifikasi.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memverifikasi email: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPhoneVerification()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        if (_securityBypassOptions.BypassPhoneVerification || !IsSmsVerificationAvailable())
        {
            TempData["ErrorMessage"] = "Verifikasi nomor HP sementara dinonaktifkan.";
            return RedirectToAction(nameof(Settings));
        }

        if (IsCooldownActive(PhoneVerificationCooldownKey, out var phoneRemaining))
        {
            TempData["ErrorMessage"] = $"Tunggu {phoneRemaining} detik sebelum mengirim ulang OTP.";
            TempData["ShowPhoneVerify"] = "1";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            var code = await _accountService.GeneratePhoneVerificationCodeAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            await _accountService.SendPhoneVerificationCodeAsync(
                user.Id,
                code,
                HttpContext.RequestAborted
            );
            TempData["ShowPhoneVerify"] = "1";
            if (_smsOptions.UseDevelopmentMode)
            {
                TempData["DevPhoneOtp"] = code;
            }
            TempData["SuccessMessage"] =
                $"Kode OTP telah dikirim ke {user.PhoneNumber ?? "nomor HP Anda"}.";
            StartCooldown(PhoneVerificationCooldownKey);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim OTP SMS: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhone(string code)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        if (_securityBypassOptions.BypassPhoneVerification || !IsSmsVerificationAvailable())
        {
            TempData["ErrorMessage"] = "Verifikasi nomor HP sementara dinonaktifkan.";
            return RedirectToAction(nameof(Settings));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Kode OTP wajib diisi.";
            TempData["ShowPhoneVerify"] = "1";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            await _accountService.ConfirmPhoneAsync(user.Id, code, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Nomor HP berhasil diverifikasi.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memverifikasi nomor HP: {ex.Message}";
            TempData["ShowPhoneVerify"] = "1";
        }

        return RedirectToAction(nameof(Settings));
    }
}
